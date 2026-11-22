"""Finds non-public symbols (leading underscore) and the references production code makes to them."""

import ast
from dataclasses import dataclass

from gate_python import signature
from gate_python.model import ClassInfo, FunctionInfo, Module, Program
from gate_python.syntax import body_nodes, is_dunder, own_nodes

MODULE_LEVEL = "module-level"
MEMBER = "member"


@dataclass(frozen=True)
class Private:
    """One non-public symbol: where it is declared and the lines its own definition covers."""

    name: str
    path: str
    line: int
    last: int
    kind: str
    anchor: str


def unreferenced(program: Program) -> list[Private]:
    production = [module for module in program.modules.values() if not module.test]
    declared = [private for module in production for private in _declared(module, program)]
    members = {node.attr for module in production for node in ast.walk(module.tree) if isinstance(node, ast.Attribute) and isinstance(node.ctx, ast.Load)}
    imported = {alias.name for module in production for node in ast.walk(module.tree) if isinstance(node, ast.ImportFrom) for alias in node.names}
    first = {(private.path, private.kind, private.name): private for private in reversed(declared)}
    return [
        private
        for private in first.values()
        if private.name not in members and not _is_named(private, program) and not (private.kind == MODULE_LEVEL and private.name in imported)
    ]


def is_private(name: str) -> bool:
    return name.startswith("_") and not is_dunder(name) and name.strip("_") != ""


def _is_named(private: Private, program: Program) -> bool:
    """True when the module names the symbol outside the symbol's own definition."""
    tree = program.modules[private.path].tree
    names = [node for node in ast.walk(tree) if isinstance(node, ast.Name) and isinstance(node.ctx, ast.Load)]
    lines = [node.lineno for node in names if node.id == private.name]
    return any(line < private.line or line > private.last for line in lines)


def _declared(module: Module, program: Program) -> list[Private]:
    functions = [function for function in program.all_functions if function.path == module.path]
    classes = [info for info in program.classes.values() if info.path == module.path]
    return [
        *(_function(function) for function in functions if is_private(function.name) and not function.parent),
        *(_class(info) for info in classes if is_private(info.node.name)),
        *(private for node in own_nodes(module.tree) for private in _variable(node, module, MODULE_LEVEL)),
        *(private for info in classes for node in own_nodes(info.node) for private in _variable(node, module, MEMBER)),
        *(private for function in functions if function.owner for private in _fields(function, module)),
    ]


def _function(function: FunctionInfo) -> Private:
    kind = MEMBER if function.owner else MODULE_LEVEL
    return Private(function.name, function.path, function.node.lineno, function.node.end_lineno or 0, kind, function.symbol)


def _class(info: ClassInfo) -> Private:
    return Private(info.node.name, info.path, info.node.lineno, info.node.end_lineno or 0, MODULE_LEVEL, info.symbol)


def _variable(node: ast.AST, module: Module, kind: str) -> list[Private]:
    match node:
        case ast.Name(id=name, ctx=ast.Store()) if is_private(name):
            return [Private(name, module.path, node.lineno, node.lineno, kind, "")]
    return []


def _fields(method: FunctionInfo, module: Module) -> list[Private]:
    receiver = signature.receiver(method)
    return [private for node in body_nodes(method.node) for private in _field(node, receiver, module)]


def _field(node: ast.AST, receiver: str, module: Module) -> list[Private]:
    match node:
        case ast.Attribute(value=ast.Name(id=owner), attr=name, ctx=ast.Store()) if owner == receiver and is_private(name):
            return [Private(name, module.path, node.lineno, node.lineno, MEMBER, "")]
    return []
