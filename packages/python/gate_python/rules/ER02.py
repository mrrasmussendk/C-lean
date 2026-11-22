"""ER02: Null must not be returned or passed where the type system can express absence. The language's strict null checking is enabled with its warnings as errors."""

from gate_python import nulls
from gate_python.findings import on_line
from gate_python.model import Analysis, Finding

RULE = "ER02"
ADVICE = "Declare the type as optional, return an empty value, or turn strict checking on"


def check(analysis: Analysis) -> list[Finding]:
    uses = nulls.null_uses(analysis)
    return [on_line(RULE, use.path, use.line, f"{use.what}. {ADVICE}") for use in uses]
