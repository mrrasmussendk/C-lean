"""DS01: The same discriminator must not select a branch in more functions than the limit."""

from gate_python import discriminators
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo

RULE = "DS01"
ADVICE = "Give the cases a type of their own or one function that decides"


def check(analysis: Analysis) -> list[Finding]:
    branchings = discriminators.branchings(analysis.program)
    limit = analysis.config.limits[RULE]
    return [
        on_function(
            RULE, found.functions[0], f"{_problem(found.discriminator, found.functions)}, limit {limit}. {ADVICE}"
        )
        for found in branchings
        if len(found.functions) > limit
    ]


def _problem(discriminator: str, functions: tuple[FunctionInfo, ...]) -> str:
    places = ", ".join(f"{function.name} ({function.path}:{function.node.lineno})" for function in functions)
    return f"discriminator {discriminator} selects a branch in {len(functions)} functions: {places}"
