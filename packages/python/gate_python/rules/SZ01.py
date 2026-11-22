"""SZ01: Function body SLOC must not exceed the limit."""

from gate_python import sloc
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo

RULE = "SZ01"
ADVICE = "Split it at a point where the extracted part has a name that is not a restatement of its body"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    measured = sloc.body_sloc(function, analysis.program)
    limit = analysis.config.limits[RULE]
    if measured <= limit:
        return []
    return [on_function(RULE, function, f"function {function.name} has {measured} SLOC, limit {limit}. {ADVICE}")]
