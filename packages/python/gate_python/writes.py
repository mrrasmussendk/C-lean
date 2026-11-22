"""Finds where a function writes into an object it received as a parameter."""

import ast
from dataclasses import dataclass

from gate_python import signature
from gate_python.model import FunctionInfo, Scope
from gate_python.syntax import body_nodes, is_member_write, root_name

MUTATORS = frozenset({
    "append", "extend", "insert", "add", "update", "setdefault", "pop", "popitem", "remove", "discard", "clear",
    "sort", "reverse", "appendleft", "extendleft", "popleft", "difference_update", "intersection_update",
    "symmetric_difference_update",
})
COLLECTIONS = frozenset({
    "?", "builtins.list", "builtins.dict", "builtins.set", "builtins.bytearray", "collections.deque",
    "collections.defaultdict", "collections.OrderedDict", "collections.Counter", "typing.List", "typing.Dict",
    "typing.Set", "typing.Deque", "typing.DefaultDict", "typing.MutableSequence", "typing.MutableMapping",
    "typing.MutableSet", "collections.abc.MutableSequence", "collections.abc.MutableMapping",
    "collections.abc.MutableSet",
})


@dataclass(frozen=True)
class Write:
    parameter: str
    line: int
    how: str


def parameter_writes(function: FunctionInfo, scope: Scope) -> list[Write]:
    """Member and element assignments through a parameter, and collection mutators called on one."""
    nodes = list(body_nodes(function.node))
    rebound = {node.id for node in nodes if isinstance(node, ast.Name) and isinstance(node.ctx, ast.Store)}
    names = {parameter.arg for parameter in signature.parameters(function)} - rebound
    found = [write for node in nodes for write in _write(node, scope)]
    return [write for write in found if write.parameter in names]


def _write(node: ast.AST, scope: Scope) -> list[Write]:
    match node:
        case ast.Attribute() | ast.Subscript() if is_member_write(node):
            return [Write(root_name(node), node.lineno, "assigns into")]
        case ast.Call(func=ast.Attribute(value=ast.Name(id=name), attr=method)) if method in MUTATORS:
            known = scope.env[name].name in COLLECTIONS if name in scope.env else False
            return [Write(name, node.lineno, f"calls {method} on")] if known else []
    return []
