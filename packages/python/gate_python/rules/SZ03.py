"""SZ03: Nesting depth of control structures in a function must not exceed the limit."""

from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo
from gate_python.nesting import nesting_depth

RULE = "SZ03"
ADVICE = "Return early or move the innermost block into a named function"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    measured = nesting_depth(function.node)
    limit = analysis.config.limits[RULE]
    if measured <= limit:
        return []
    return [on_function(RULE, function, f"function {function.name} nests control structures {measured} deep, limit {limit}. {ADVICE}")]
