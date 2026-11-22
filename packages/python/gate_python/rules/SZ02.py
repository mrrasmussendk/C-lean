"""SZ02: Cognitive complexity of a function must not exceed the limit."""

from gate_python.complexity import cognitive_complexity
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo

RULE = "SZ02"
ADVICE = "Flatten nested branches with early returns or move a branch with its own purpose into a named function"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    measured = cognitive_complexity(function.node, function.name)
    limit = analysis.config.limits[RULE]
    if measured <= limit:
        return []
    return [on_function(RULE, function, f"function {function.name} has cognitive complexity {measured}, limit {limit}. {ADVICE}")]
