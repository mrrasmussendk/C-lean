"""DC01: A non-public symbol must be referenced by production code."""

from gate_python import privates
from gate_python.model import Analysis, Finding

RULE = "DC01"
ADVICE = "Delete it, or use it from production code"


def check(analysis: Analysis) -> list[Finding]:
    unused = privates.unreferenced(analysis.program)
    return [
        Finding(
            RULE,
            private.path,
            private.line,
            f"{RULE}: {private.kind} symbol {private.name} is referenced by no production code. {ADVICE}.",
            private.anchor,
        )
        for private in unused
    ]
