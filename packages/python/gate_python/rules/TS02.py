"""TS02: A test must not assert only on values it configured a test double to return."""

import ast

from gate_python import scopes, testing
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo

RULE = "TS02"
WHAT = "all on values returned by its own test doubles"
ADVICE = "Assert on what the code under test returns or does"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    names = testing.doubles(function, scopes.scope_of(function, analysis.program))
    results = testing.results_of(function, names)
    found = testing.assertions(function)
    on_doubles = [node for node in found if any(_is_double_value(value, names, results) for value in _asserted(node))]
    if not testing.is_test(function) or not found or len(on_doubles) < len(found):
        return []
    return [
        on_function(
            RULE,
            function,
            f"test {function.name} makes {len(found)} assertions, {WHAT}. {ADVICE}",
        )
    ]


def _asserted(assertion: ast.AST) -> list[ast.expr]:
    """The values an assertion compares."""
    match assertion:
        case ast.Assert(test=ast.Compare(left=left, comparators=others)):
            return [left, *others]
        case ast.Assert(test=ast.UnaryOp(operand=value)) | ast.Assert(test=value):
            return [value]
        case ast.Call(args=values):
            return list(values)
    return []


def _is_double_value(value: ast.expr, names: set[str], results: set[str]) -> bool:
    return testing.is_call_on(value, names) or (isinstance(value, ast.Name) and value.id in results)
