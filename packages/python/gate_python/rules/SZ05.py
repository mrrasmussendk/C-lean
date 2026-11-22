"""SZ05: Type SLOC must not exceed the limit."""

from gate_python import sloc
from gate_python.findings import on_type
from gate_python.model import Analysis, ClassInfo, Finding

RULE = "SZ05"
ADVICE = "Move a group of members that change together into a type of their own"


def check(info: ClassInfo, analysis: Analysis) -> list[Finding]:
    measured = sloc.type_sloc(info, analysis.program)
    limit = analysis.config.limits[RULE]
    if measured <= limit:
        return []
    return [on_type(RULE, info, f"type {info.node.name} has {measured} SLOC, limit {limit}. {ADVICE}")]
