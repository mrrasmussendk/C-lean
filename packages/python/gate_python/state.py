"""Finds where a function assigns instance state, and static or module-level state."""

import ast

from gate_python import signature
from gate_python.bindings import in_root
from gate_python.model import FunctionInfo, Scope
from gate_python.resolve import qualified_of
from gate_python.syntax import body_nodes, declared_outside, is_member_write, root_name


def instance_writes(function: FunctionInfo) -> list[ast.expr]:
    """Assignments to members or elements of the receiver."""
    name = signature.receiver(function)
    if not name or signature.is_class_method(function):
        return []
    written = [node for node in body_nodes(function.node) if isinstance(node, ast.expr) and is_member_write(node)]
    return [node for node in written if root_name(node) == name]


def global_writes(function: FunctionInfo, scope: Scope) -> list[ast.expr]:
    """Assignments to names declared global, and to members or elements of classes and module variables."""
    outside = declared_outside(function.node)
    written = [node for node in body_nodes(function.node) if isinstance(node, ast.expr)]
    return [node for node in written if _is_global_write(node, outside) or _is_shared_write(node, function, scope)]


def _is_global_write(node: ast.AST, outside: frozenset[str]) -> bool:
    match node:
        case ast.Name(id=name, ctx=ast.Store()):
            return name in outside
    return False


def _is_shared_write(node: ast.AST, function: FunctionInfo, scope: Scope) -> bool:
    if not is_member_write(node):
        return False
    name = root_name(node)
    if name and name == signature.receiver(function):
        return signature.is_class_method(function)
    owner = qualified_of(ast.Name(name, ast.Load()), scope) if name else ""
    return bool(owner) and in_root(owner, scope.program)


def state_writes(function: FunctionInfo, scope: Scope) -> list[ast.expr]:
    return [*instance_writes(function), *global_writes(function, scope)]
