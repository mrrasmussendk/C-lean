"""Nesting depth of control structures: if, loop, match and try. An else-if does not nest."""

import ast
from typing import Sequence

from gate_python.model import FunctionNode
from gate_python.syntax import SCOPES, body_of

CONTROL = (ast.If, ast.For, ast.AsyncFor, ast.While, ast.Match, ast.Try, ast.TryStar)


def nesting_depth(function: FunctionNode) -> int:
    return _deepest(body_of(function), 0)


def _deepest(nodes: Sequence[ast.AST], depth: int) -> int:
    return max([depth, *(_depth(node, depth) for node in nodes)])


def _depth(node: ast.AST, depth: int) -> int:
    """The deepest level reached at or below the node; a nested function starts again and is not counted here."""
    match node:
        case ast.If(test=test, body=body, orelse=[ast.If() as chained]):
            return max(_deepest([test, *body], depth + 1), _depth(chained, depth))
    if isinstance(node, SCOPES):
        return depth
    inside = depth + 1 if isinstance(node, CONTROL) else depth
    return _deepest(list(ast.iter_child_nodes(node)), inside)
