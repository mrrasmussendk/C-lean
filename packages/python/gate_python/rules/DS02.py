"""DS02: The same group of parameters must not travel through more functions than the limit."""

from gate_python import clumps
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo

RULE = "DS02"
ADVICE = "Make the group a type and pass that"


def check(analysis: Analysis) -> list[Finding]:
    limit = analysis.config.limits[RULE]
    travelling = clumps.clumps(analysis.program, limit)
    return [
        on_function(
            RULE,
            clump.functions[0],
            f"{_problem(clumps.pairs_text(clump.group), clump.functions)}, limit {limit}. {ADVICE}",
        )
        for clump in travelling
    ]


def _problem(group: str, functions: tuple[FunctionInfo, ...]) -> str:
    places = ", ".join(f"{function.name} ({function.path}:{function.node.lineno})" for function in functions)
    return f"parameter group ({group}) travels through {len(functions)} functions: {places}"
