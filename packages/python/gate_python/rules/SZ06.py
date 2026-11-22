"""SZ06: A source line must not exceed the limit in characters."""

from gate_python.findings import on_line
from gate_python.model import Analysis, Finding, Module

RULE = "SZ06"
ADVICE = "Wrap the line, or name a part of the expression"


def check(module: Module, analysis: Analysis) -> list[Finding]:
    measured = [(number, len(line)) for number, line in enumerate(module.lines, start=1)]
    limit = analysis.config.limits[RULE]
    return [
        on_line(RULE, module.path, number, f"line {number} is {length} characters long, limit {limit}. {ADVICE}")
        for number, length in measured
        if length > limit
    ]
