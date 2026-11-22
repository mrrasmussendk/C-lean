"""Cognitive complexity as defined by SonarSource (G. Ann Campbell, 2017)."""

import ast
from dataclasses import dataclass

from gate_python.model import FunctionNode
from gate_python.syntax import body_of

NESTING = (ast.For, ast.AsyncFor, ast.While, ast.IfExp, ast.Match, ast.ExceptHandler)
FUNCTIONS = (ast.FunctionDef, ast.AsyncFunctionDef, ast.Lambda)
RECEIVERS = ("self", "cls")


@dataclass(frozen=True)
class Level:
    nesting: int
    name: str


def cognitive_complexity(function: FunctionNode, name: str) -> int:
    return sum(_cost(part, Level(0, name)) for part in body_of(function))


def _cost(node: ast.AST, level: Level) -> int:
    if isinstance(node, ast.If):
        return _if_cost(node, level, 1 + level.nesting)
    if isinstance(node, NESTING):
        return 1 + level.nesting + _inside(node, _deeper(level))
    if isinstance(node, FUNCTIONS):
        return _inside(node, _deeper(level))
    if isinstance(node, ast.BoolOp):
        return 1 + _inside(node, level)
    if isinstance(node, ast.Call) and _is_recursion(node.func, level.name):
        return 1 + _inside(node, level)
    return _inside(node, level)


def _if_cost(node: ast.If, level: Level, own: int) -> int:
    """An if costs one plus its nesting; an else-if and an else cost one each."""
    total = own + _cost(node.test, level) + _block(node.body, _deeper(level))
    match node.orelse:
        case []:
            return total
        case [ast.If() as chained]:
            return total + _if_cost(chained, level, 1)
    return total + 1 + _block(node.orelse, _deeper(level))


def _block(statements: list[ast.stmt], level: Level) -> int:
    return sum(_cost(statement, level) for statement in statements)


def _inside(node: ast.AST, level: Level) -> int:
    return sum(_cost(child, level) for child in ast.iter_child_nodes(node))


def _deeper(level: Level) -> Level:
    return Level(level.nesting + 1, level.name)


def _is_recursion(called: ast.expr, name: str) -> bool:
    match called:
        case ast.Name(id=target):
            return target == name
        case ast.Attribute(value=ast.Name(id=owner), attr=target):
            return target == name and owner in RECEIVERS
    return False
