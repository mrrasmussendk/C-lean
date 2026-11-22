"""FX03: A function must not mutate an object it received as an argument."""

from gate_python import scopes, writes
from gate_python.findings import on_line
from gate_python.model import Analysis, Finding, FunctionInfo

RULE = "FX03"
ADVICE = "Build and return a new object, or make the change a method of the object's own type"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    mutations = writes.parameter_writes(function, scopes.scope_of(function, analysis.program))
    return [
        on_line(RULE, function.path, write.line, f"function {function.name} {write.how} its argument {write.parameter}. {ADVICE}")
        for write in mutations
    ]
