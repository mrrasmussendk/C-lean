"""Counts source lines: lines that are neither blank, comment-only nor documentation string."""

from gate_python.model import ClassInfo, FunctionInfo, Program
from gate_python.syntax import body_of


def body_sloc(function: FunctionInfo, program: Program) -> int:
    body = body_of(function.node)
    return _between(program.modules[function.path].code_lines, body[0].lineno, body[-1].end_lineno or body[-1].lineno)


def type_sloc(info: ClassInfo, program: Program) -> int:
    """From the declaration line to the closing line of the type."""
    return _between(program.modules[info.path].code_lines, info.node.lineno, info.node.end_lineno or info.node.lineno)


def _between(code_lines: frozenset[int], first: int, last: int) -> int:
    return len([line for line in code_lines if first <= line <= last])
