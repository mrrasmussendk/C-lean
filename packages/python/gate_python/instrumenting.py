"""Rewrites a module so that every function makes its file's component current while it runs."""

import ast
import json
from dataclasses import dataclass
from typing import Any

from gate_python.model import Module
from gate_python.runtime_names import HELPER_MODULE, TRACE_FILE_PLACEHOLDER
from gate_python.syntax import body_nodes, has_docstring

PREVIOUS = "_gate_previous"
SENT = "_gate_sent"
HELD = "_gate_held"
HOLE = "_gate_hole"
SENT_INDEX = 2
FUTURE = "__future__"
YIELDS = (ast.Yield, ast.YieldFrom)


@dataclass(frozen=True)
class Site:
    """What the rewriting of one file needs: its component as a literal."""

    component: str


def helper_text(template: str, trace_file: str) -> str:
    """The runtime helper with the trace file path compiled in."""
    return template.replace(json.dumps(TRACE_FILE_PLACEHOLDER), json.dumps(trace_file))


def rewritten(module: Module, root: str) -> str:
    site = Site(repr(module.component))
    body = [_rebuilt(statement, site) for statement in module.tree.body]
    lead = len([statement for statement in body if _stays_first(statement, body)])
    fields: dict[str, Any] = {"body": [*body[:lead], *_preamble(root), *body[lead:]], "type_ignores": []}
    tree = ast.Module(**fields)
    return ast.unparse(ast.fix_missing_locations(tree)) + "\n"


def _stays_first(statement: ast.AST, body: list[ast.AST]) -> bool:
    """True for the module documentation string and for __future__ imports, which must stay on top."""
    match statement:
        case ast.ImportFrom(module=origin):
            return origin == FUTURE
        case ast.Expr(value=ast.Constant(value=str())):
            return statement is body[0]
    return False


def _preamble(root: str) -> list[ast.stmt]:
    """Makes the helper under the root importable from any file, then imports it."""
    lines = [
        "import sys as _gate_sys",
        f"if {root!r} not in _gate_sys.path:",
        f"    _gate_sys.path.append({root!r})",
        f"import {HELPER_MODULE}",
    ]
    return ast.parse("\n".join(lines)).body


def _rebuilt(node: ast.AST, site: Site) -> ast.AST:
    """A copy of the node with every function, lambda and yield below it instrumented."""
    if isinstance(node, ast.Lambda) and _has_yield(node):
        return node
    fields = {name: _rebuilt_value(value, site) for name, value in ast.iter_fields(node)}
    copy = type(node)(**fields)
    match copy:
        case ast.FunctionDef() | ast.AsyncFunctionDef():
            return _with_guarded_body(copy, site)
        case ast.Lambda(body=body):
            return _replaced(copy, "body", _expression(f"{HELPER_MODULE}.call({site.component}, lambda: {HOLE})", body))
        case ast.Yield() | ast.YieldFrom():
            return _resuming(copy, site)
    return copy


def _rebuilt_value(value: object, site: Site) -> object:
    match value:
        case ast.AST():
            return _rebuilt(value, site)
        case list():
            return [_rebuilt_value(item, site) for item in value]
    return value


def _has_yield(function: ast.Lambda) -> bool:
    return any(isinstance(node, YIELDS) for node in body_nodes(function))


def _with_guarded_body(function: ast.FunctionDef | ast.AsyncFunctionDef, site: Site) -> ast.AST:
    """The function with its body inside try/finally: enter the component first, restore the previous one last."""
    lead = function.body[:1] if has_docstring(function.body) else []
    rest = function.body[len(lead):] or [ast.Pass()]
    enter = ast.parse(f"{PREVIOUS} = {HELPER_MODULE}.enter({site.component})").body
    leave = ast.parse(f"{HELPER_MODULE}.leave({PREVIOUS})").body
    guarded = ast.Try(body=rest, handlers=[], orelse=[], finalbody=leave)
    return _replaced(function, "body", [*lead, *enter, guarded])


def _replaced(node: ast.AST, field: str, value: object) -> ast.AST:
    """A copy of the node with one field replaced."""
    fields: dict[str, Any] = dict(ast.iter_fields(node)) | {field: value}
    return type(node)(**fields)


def _resuming(suspension: ast.Yield | ast.YieldFrom, site: Site) -> ast.AST:
    """A yield that gives the component back while suspended and takes it again when resumed."""
    value = suspension.value or ast.Constant(value=None)
    handed = _expression(f"{HELPER_MODULE}.suspend({HELD}, {HOLE})", value)
    steps = [f"({HELD} := {PREVIOUS})", f"({PREVIOUS} := None)", f"({SENT} := {HOLE})", f"({PREVIOUS} := {HELPER_MODULE}.enter({site.component}))"]
    return _expression(f"({', '.join(steps)})[{SENT_INDEX}]", _replaced(suspension, "value", handed))


def _expression(template: str, hole: ast.AST) -> ast.AST:
    """The template expression with its hole replaced by the given node."""
    parsed = ast.parse(template, mode="eval").body
    return _filled(parsed, hole)


def _filled(node: ast.AST, hole: ast.AST) -> ast.AST:
    match node:
        case ast.Name(id=name) if name == HOLE:
            return hole
    fields = {name: _filled_value(value, hole) for name, value in ast.iter_fields(node)}
    return type(node)(**fields)


def _filled_value(value: object, hole: ast.AST) -> object:
    match value:
        case ast.AST():
            return _filled(value, hole)
        case list():
            return [_filled_value(item, hole) for item in value]
    return value
