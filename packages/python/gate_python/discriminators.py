"""Finds the discriminators a function branches on: named constants, enum types and tested types."""

import ast
from dataclasses import dataclass
from itertools import chain

from gate_python import members, scopes
from gate_python.bindings import in_root, split_module
from gate_python.model import FunctionInfo, Program, Scope
from gate_python.resolve import qualified_of, type_of, unwrapped
from gate_python.syntax import body_nodes

ORDERING = (ast.Eq, ast.NotEq, ast.Lt, ast.LtE, ast.Gt, ast.GtE, ast.Is, ast.IsNot)
TYPE_TEST = "builtins.isinstance"


@dataclass(frozen=True)
class Branching:
    """One discriminator and the production functions that branch on it, in file and line order."""

    discriminator: str
    functions: tuple[FunctionInfo, ...]


def branchings(program: Program) -> list[Branching]:
    production = [function for function in program.all_functions if not program.modules[function.path].test]
    ordered = sorted(production, key=_place)
    used = [(function, discriminators_of(function, program)) for function in ordered]
    names = sorted(set(chain.from_iterable(found for _, found in used)))
    return [Branching(name, tuple(function for function, found in used if name in found)) for name in names]


def _place(function: FunctionInfo) -> tuple[str, int]:
    return (function.path, function.node.lineno)


def discriminators_of(function: FunctionInfo, program: Program) -> set[str]:
    scope = scopes.scope_of(function, program)
    conditions = chain.from_iterable(_conditions(node) for node in body_nodes(function.node))
    tested = chain.from_iterable(ast.walk(condition) for condition in conditions)
    return set(chain.from_iterable(_discriminators(node, scope) for node in tested))


def _conditions(node: ast.AST) -> list[ast.AST]:
    """The condition of an if or conditional expression; the subject and the case patterns of a match."""
    match node:
        case ast.If(test=condition) | ast.IfExp(test=condition):
            return [condition]
        case ast.Match(subject=subject, cases=cases):
            return [subject, *(case.pattern for case in cases)]
    return []


def _discriminators(node: ast.AST, scope: Scope) -> list[str]:
    """The discriminators one node of a condition compares with or tests for."""
    match node:
        case ast.Compare(left=left, ops=operators, comparators=others) if all(
            isinstance(op, ORDERING) for op in operators
        ):
            return list(chain.from_iterable(_compared(operand, scope) for operand in [left, *others]))
        case ast.Call(func=called, args=[_, kinds]) if qualified_of(called, scope) == TYPE_TEST:
            return [name for name in _named(kinds, scope) if name in scope.program.classes]
        case ast.MatchValue(value=value):
            return _compared(value, scope)
        case ast.MatchClass(cls=kind):
            return [name for name in _named(kind, scope) if name in scope.program.classes]
        case ast.Name():
            return _enum_of(node, scope)
    return []


def _named(node: ast.expr, scope: Scope) -> list[str]:
    parts = node.elts if isinstance(node, ast.Tuple) else [node]
    return [qualified_of(part, scope) for part in parts]


def _compared(operand: ast.expr, scope: Scope) -> list[str]:
    """The constant, the enum of the member, or the class that an operand of a comparison names."""
    name = qualified_of(operand, scope)
    owner = name.rpartition(".")[0]
    if owner in scope.program.classes and members.is_enum(owner, scope.program):
        return [owner]
    if name in scope.program.classes or _is_named_constant(name, scope.program):
        return [name]
    return []


def _is_named_constant(name: str, program: Program) -> bool:
    module, rest = split_module(name, program)
    return in_root(name, program) and bool(module) and len(rest) == 1 and rest[0].isupper()


def _enum_of(subject: ast.Name, scope: Scope) -> list[str]:
    """The enum type of a matched or tested variable, when it has one."""
    kind = unwrapped(type_of(subject, scope)).name
    return [kind] if kind in scope.program.classes and members.is_enum(kind, scope.program) else []
