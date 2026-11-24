"""Finds where a function writes into an object it received as a parameter."""

import ast
from dataclasses import dataclass

from gate_python import signature
from gate_python.model import FunctionInfo, Scope
from gate_python.resolve import unwrapped
from gate_python.syntax import body_nodes, is_member_write, root_name

MUTATORS = frozenset(
    {
        "append",
        "extend",
        "insert",
        "add",
        "update",
        "setdefault",
        "pop",
        "popitem",
        "remove",
        "discard",
        "clear",
        "sort",
        "reverse",
        "appendleft",
        "extendleft",
        "popleft",
        "difference_update",
        "intersection_update",
        "symmetric_difference_update",
    }
)
COLLECTIONS = frozenset(
    {
        "?",
        "builtins.list",
        "builtins.dict",
        "builtins.set",
        "builtins.bytearray",
        "collections.deque",
        "collections.defaultdict",
        "collections.OrderedDict",
        "collections.Counter",
        "typing.List",
        "typing.Dict",
        "typing.Set",
        "typing.Deque",
        "typing.DefaultDict",
        "typing.MutableSequence",
        "typing.MutableMapping",
        "typing.MutableSet",
        "collections.abc.MutableSequence",
        "collections.abc.MutableMapping",
        "collections.abc.MutableSet",
    }
)


KNOWN_COLLECTIONS = COLLECTIONS - {"?"}
Position = tuple[int, int]


@dataclass(frozen=True)
class Write:
    parameter: str
    line: int
    how: str
    column: int = 0


def parameter_writes(function: FunctionInfo, scope: Scope) -> list[Write]:
    """Member and element assignments through a parameter, and collection mutators called on one.

    A write counts until the name is rebound: after `items = sorted(items)` the name no longer stands for the
    caller's object, before it it does.
    """
    nodes = list(body_nodes(function.node))
    rebound = _first_rebindings(nodes)
    names = {parameter.arg for parameter in signature.parameters(function)}
    found = [write for node in nodes for write in _write(node, scope)]
    return [write for write in found if write.parameter in names and not _is_rebound(write, rebound)]


def _first_rebindings(nodes: list[ast.AST]) -> dict[str, Position]:
    """Where each name is first given a new value. An augmented assignment keeps the object and is left out."""
    augmented = {id(node.target) for node in nodes if isinstance(node, ast.AugAssign)}
    stores = [node for node in nodes if isinstance(node, ast.Name) and isinstance(node.ctx, ast.Store)]
    places = [(node.id, (node.lineno, node.col_offset)) for node in stores if id(node) not in augmented]
    return dict(reversed(sorted(places)))


def _is_rebound(write: Write, rebound: dict[str, Position]) -> bool:
    return write.parameter in rebound and rebound[write.parameter] < (write.line, write.column)


def _write(node: ast.AST, scope: Scope) -> list[Write]:
    match node:
        case ast.Attribute() | ast.Subscript() if is_member_write(node):
            return [Write(root_name(node), node.lineno, "assigns into", node.col_offset)]
        case ast.Call(func=ast.Attribute(value=ast.Name(id=name), attr=method)) if method in MUTATORS:
            known = _kind(name, scope) in COLLECTIONS
            return [Write(name, node.lineno, f"calls {method} on", node.col_offset)] if known else []
        case ast.AugAssign(target=ast.Name(id=name)) if _kind(name, scope) in KNOWN_COLLECTIONS:
            return [Write(name, node.lineno, "changes in place", node.col_offset)]
    return []


def _kind(name: str, scope: Scope) -> str:
    """The type of the name, or of what it holds when it may be None instead; nothing for a name with no type."""
    return unwrapped(scope.env[name]).name if name in scope.env else ""
