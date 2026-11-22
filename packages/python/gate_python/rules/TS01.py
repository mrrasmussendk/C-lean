"""TS01: A test must contain at least one assertion."""

from gate_python import scopes, testing
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo

RULE = "TS01"
ADVICE = "Assert on the result or the effect the test is about"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    scope = scopes.scope_of(function, analysis.program)
    found = [*testing.assertions(function), *testing.helper_assertions(function, scope)]
    if not testing.is_test(function) or found:
        return []
    return [on_function(RULE, function, f"test {function.name} contains {len(found)} assertions, at least 1 is required. {ADVICE}")]
