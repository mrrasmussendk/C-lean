"""Console output. The only module that prints."""

import sys

from gate_python.model import Problem
from gate_python.rule_table import TOOL_NAME


def say(text: str) -> None:
    print(text)


def report(problems: list[Problem]) -> None:
    """One line per problem on standard error: gate-python: <file>:<line>:<column>: <what is wrong>. <what to do>."""
    for problem in problems:
        print(f"{TOOL_NAME}: {_position(problem)}{problem.text}.", file=sys.stderr)


def _position(problem: Problem) -> str:
    if not problem.file:
        return ""
    if not problem.line:
        return f"{problem.file}: "
    return f"{problem.file}:{problem.line}:{problem.column}: "
