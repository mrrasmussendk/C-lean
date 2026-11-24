"""Finds None returned, passed or forced where the declared type does not admit it, and lax type-checker settings."""

import ast
import tomllib
from dataclasses import dataclass
from itertools import chain
from typing import Any

from gate_python import scopes, signature
from gate_python.model import (
    CLASS,
    EXTERNAL,
    FUNCTION,
    Analysis,
    FunctionInfo,
    Located,
    Module,
    Program,
    Scope,
    SourceFile,
)
from gate_python.resolve import qualified_of, target_of
from gate_python.syntax import body_nodes, parsed_expressions

ADMITTING = ("None", "typing.Optional", "typing.Any", "builtins.object")
UNION = "typing.Union"
ANNOTATED = "typing.Annotated"
CAST = "typing.cast"
CONSTRUCTOR = "__init__"
LAX = "the type checker is not in strict mode with its warnings as errors"
LAX_LEVELS = ("warning", "information", "none", False)


@dataclass(frozen=True)
class NullUse:
    path: str
    line: int
    what: str


def null_uses(analysis: Analysis) -> list[NullUse]:
    program = analysis.program
    production = [module for module in program.modules.values() if not module.test]
    functions = [function for function in program.all_functions if not program.modules[function.path].test]
    returned = chain.from_iterable(_returned(function, program) for function in functions)
    located = chain.from_iterable(scopes.scoped_nodes(module, program) for module in production)
    passed = chain.from_iterable(_passed(item) for item in located)
    return [
        *returned,
        *passed,
        *(NullUse(project.path, 1, LAX) for project in analysis.projects if not _is_strict(project)),
    ]


def admits_none(annotation: ast.expr | None, scope: Scope) -> bool:
    """True when the declared type admits None, or when no type is declared."""
    match annotation:
        case None | ast.Constant(value=None):
            return True
        case ast.Constant(value=str() as text):
            return admits_none(next(iter(parsed_expressions(text)), None), scope)
        case ast.BinOp(left=left, right=right):
            return admits_none(left, scope) or admits_none(right, scope)
        case ast.Subscript(value=head, slice=inner):
            return _generic_admits(qualified_of(head, scope), inner, scope)
    name = qualified_of(annotation, scope)
    return name in ADMITTING or (name not in scope.program.classes and not _is_external_type(name, scope.program))


def _is_external_type(name: str, program: Program) -> bool:
    return bool(name) and name.split(".")[0] not in program.namespaces


def _generic_admits(head: str, inner: ast.expr, scope: Scope) -> bool:
    parts = list(inner.elts) if isinstance(inner, ast.Tuple) else [inner]
    if head == UNION:
        return any(admits_none(part, scope) for part in parts)
    if head == ANNOTATED:
        return admits_none(parts[0], scope)
    return head in ADMITTING


def _returned(function: FunctionInfo, program: Program) -> list[NullUse]:
    if isinstance(function.node, ast.Lambda) or admits_none(function.node.returns, scopes.scope_of(function, program)):
        return []
    declared = ast.unparse(function.node.returns) if function.node.returns else ""
    nodes = list(body_nodes(function.node))
    if any(isinstance(node, (ast.Yield, ast.YieldFrom)) for node in nodes):
        return []
    returns = [node for node in nodes if isinstance(node, ast.Return) and _may_be_none(node.value)]
    return [
        NullUse(function.path, node.lineno, f"function {function.name} returns None but declares {declared}")
        for node in returns
    ]


def _may_be_none(returned: ast.expr | None) -> bool:
    """A return without a value, of None, or of a conditional expression one of whose branches is None.

    A generator is not asked: what it returns is not what its declared type describes.
    """
    match returned:
        case None | ast.Constant(value=None):
            return True
        case ast.IfExp(body=first, orelse=second):
            return _may_be_none(first) or _may_be_none(second)
    return False


def _is_none(node: ast.AST | None) -> bool:
    return isinstance(node, ast.Constant) and node.value is None


def _passed(item: Located) -> list[NullUse]:
    call = item.node
    if not isinstance(call, ast.Call):
        return []
    target = target_of(call.func, item.scope)
    if target.kind == EXTERNAL and target.name == CAST:
        return _forced(call, item.scope)
    declared = _declared_parameters(target.kind, target.name, item.scope.program)
    given = [*zip(call.args, declared), *((keyword.value, _named(declared, keyword.arg)) for keyword in call.keywords)]
    callee = _scope_of_name(target.name, item.scope.program)
    return [
        NullUse(
            item.scope.module.path,
            value.lineno,
            f"None is passed to parameter {parameter.arg} of {target.name}, declared {ast.unparse(parameter)}",
        )
        for value, parameter in given
        if _is_none(value) and parameter is not None and not admits_none(parameter.annotation, callee)
    ]


def _forced(call: ast.Call, scope: Scope) -> list[NullUse]:
    match call.args:
        case [kind, ast.Constant(value=None) as value] if not admits_none(kind, scope):
            return [
                NullUse(
                    scope.module.path, value.lineno, f"None is cast to {ast.unparse(kind)}, which does not admit it"
                )
            ]
    return []


def _declared_parameters(kind: str, name: str, program: Program) -> list[ast.arg]:
    """The positional parameters a call to the function or class binds, in order."""
    if kind == FUNCTION:
        return signature.positional(program.functions[name])
    if kind == CLASS and f"{name}.{CONSTRUCTOR}" in program.functions:
        return signature.positional(program.functions[f"{name}.{CONSTRUCTOR}"])
    if kind == CLASS:
        return [parameter for statement in program.classes[name].node.body for parameter in _field(statement)]
    return []


def _field(statement: ast.stmt) -> list[ast.arg]:
    """A data-record field, as the constructor parameter it becomes."""
    match statement:
        case ast.AnnAssign(target=ast.Name(id=name), annotation=annotation):
            return [ast.arg(name, annotation)]
    return []


def _named(declared: list[ast.arg], name: str | None) -> ast.arg | None:
    matching = [parameter for parameter in declared if parameter.arg == name]
    return matching[0] if matching else None


def _scope_of_name(name: str, program: Program) -> Scope:
    owner = program.functions.get(name) or program.classes.get(name)
    module: Module = program.modules[owner.path] if owner else next(iter(program.modules.values()))
    return Scope(program, module, {})


def _is_strict(project: SourceFile) -> bool:
    """True when pyproject.toml sets pyright or mypy to strict and downgrades no check."""
    try:
        tools: dict[str, Any] = tomllib.loads(project.text).get("tool", {})
    except tomllib.TOMLDecodeError:
        return False
    pyright: dict[str, Any] = tools.get("pyright", {})
    lax = [name for name, level in pyright.items() if name.startswith("report") and level in LAX_LEVELS]
    return (pyright.get("typeCheckingMode") == "strict" and not lax) or tools.get("mypy", {}).get("strict") is True
