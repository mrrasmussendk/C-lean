"""Finds groups of parameters that travel together through several functions."""

import ast
from dataclasses import dataclass

from gate_python import members, signature
from gate_python.model import FunctionInfo, Program

SMALLEST_GROUP = 3
UNDECLARED = "?"


@dataclass(frozen=True)
class Clump:
    """A group of (name, declared type) parameters and the functions that declare all of them, in file and line order."""

    group: frozenset[tuple[str, str]]
    functions: tuple[FunctionInfo, ...]


def clumps(program: Program, limit: int) -> list[Clump]:
    """The largest groups that more functions than the limit declare; a group inside a reported one is left out."""
    production = [function for function in program.all_functions if not program.modules[function.path].test]
    counted = sorted([function for function in production if not _overrides(function, program)], key=_place)
    declared = [(function, _parameters(function)) for function in counted]
    wide = [group for _, group in declared if len(group) >= SMALLEST_GROUP]
    shared = _shared(_shared(set(wide), wide), wide)
    found = [Clump(group, tuple(function for function, own in declared if group <= own)) for group in shared]
    travelling = sorted([clump for clump in found if len(clump.functions) > limit], key=_size_then_place)
    return [
        clump
        for index, clump in enumerate(travelling)
        if not any(clump.group < larger.group for larger in travelling[:index])
    ]


def _place(function: FunctionInfo) -> tuple[str, int]:
    return (function.path, function.node.lineno)


def _size_then_place(clump: Clump) -> tuple[int, str, int]:
    return (-len(clump.group), *_place(clump.functions[0]))


def _parameters(function: FunctionInfo) -> frozenset[tuple[str, str]]:
    return frozenset((parameter.arg, _declared_type(parameter)) for parameter in signature.parameters(function))


def _declared_type(parameter: ast.arg) -> str:
    return ast.unparse(parameter.annotation) if parameter.annotation else UNDECLARED


def _shared(
    groups: set[frozenset[tuple[str, str]]], declared: list[frozenset[tuple[str, str]]]
) -> set[frozenset[tuple[str, str]]]:
    """The groups, and what each has in common with each declared parameter list, when that is a group."""
    common = {group & other for group in groups for other in declared}
    return groups | {group for group in common if len(group) >= SMALLEST_GROUP}


def _overrides(function: FunctionInfo, program: Program) -> bool:
    """True when a base class of the method's class declares a method of the same name."""
    if not function.owner:
        return False
    bases = members.ancestors(function.owner, program)[1:]
    return any(f"{base}.{function.name}" in program.functions for base in bases)


def pairs_text(group: frozenset[tuple[str, str]]) -> str:
    return ", ".join(name if kind == UNDECLARED else f"{name}: {kind}" for name, kind in sorted(group))
