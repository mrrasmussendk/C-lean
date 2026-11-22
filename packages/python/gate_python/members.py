"""Looks members up on classes defined under the root, through their base classes."""

from gate_python.bindings import canonical
from gate_python.model import FUNCTION, UNRESOLVED, VALUE, Program, Target, TypeRef
from gate_python.syntax import is_dunder

ENUM_MODULE = "enum."
UNKNOWN = TypeRef("")


def ancestors(qualified: str, program: Program) -> list[str]:
    """The class and every base class, nearest first; bases outside the root are named too."""
    seen: list[str] = []
    pending = [qualified]
    while pending:
        current = canonical(pending.pop(0), program)
        if current not in seen:
            seen.append(current)
            pending += _bases(current, program)
    return seen


def _bases(qualified: str, program: Program) -> list[str]:
    return list(program.classes[qualified].bases) if qualified in program.classes else []


def is_enum(qualified: str, program: Program) -> bool:
    return any(name.startswith(ENUM_MODULE) for name in ancestors(qualified, program))


def is_open(qualified: str, program: Program) -> bool:
    """True when a base class lies outside the root, so its members are not known."""
    return any(name not in program.classes for name in ancestors(qualified, program))


def lookup(qualified: str, member: str, program: Program) -> Target:
    for current in ancestors(qualified, program):
        if f"{current}.{member}" in program.functions:
            return Target(FUNCTION, f"{current}.{member}")
        if member in program.fields.get(current, {}):
            return Target(VALUE, f"{current}.{member}")
    if is_open(qualified, program) or is_dunder(member):
        return Target(VALUE, "")
    return Target(UNRESOLVED, f"{qualified}.{member}")


def field_type(qualified: str, member: str, program: Program) -> TypeRef:
    declaring = [name for name in ancestors(qualified, program) if member in program.fields.get(name, {})]
    return program.fields[declaring[0]][member] if declaring else UNKNOWN


def methods_of(qualified: str, program: Program) -> list[str]:
    """Names of the methods the class itself declares, without special methods."""
    declared = [function.name for function in program.all_functions if function.owner == qualified]
    return [name for name in declared if not is_dunder(name)]
