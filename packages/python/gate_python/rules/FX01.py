"""FX01: Effect APIs (clock, file system, network, console, randomness, environment, mutable global state) may be called only inside components marked as effect components in the architecture model."""

from gate_python import effect_uses
from gate_python.findings import on_line
from gate_python.model import Analysis, Finding, Module

RULE = "FX01"
ADVICE = "Move the effect into an effect component and pass its result in"


def check(module: Module, analysis: Analysis) -> list[Finding]:
    uses = effect_uses.in_module(module, analysis.program)
    if module.effect:
        return []
    where = f"component '{module.component}', which is not an effect component" if module.component else "a file no component maps"
    return [on_line(RULE, module.path, use.line, f"{use.api} is a {use.group} effect used in {where}. {ADVICE}") for use in uses]
