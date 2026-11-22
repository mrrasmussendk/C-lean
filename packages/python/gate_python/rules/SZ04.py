"""SZ04: A function must not declare more parameters than the limit."""

from gate_python import signature
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo

RULE = "SZ04"
ADVICE = "Group parameters that travel together into one value, or split the function"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    measured = len(signature.parameters(function))
    limit = analysis.config.limits[RULE]
    if measured <= limit:
        return []
    return [on_function(RULE, function, f"function {function.name} declares {measured} parameters, limit {limit}. {ADVICE}")]
