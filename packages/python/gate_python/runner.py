"""Runs every rule on the code it applies to and marks the findings that a valid suppression silences."""

from gate_python import suppress
from gate_python.model import Analysis, Finding, Module, Result
from gate_python.rules import HD01
from gate_python.rules.registry import FILE_RULES, FUNCTION_RULES, PROGRAM_RULES, TYPE_RULES
from gate_python.suppress import Suppression

TEST_PREFIX = "TS"
EVERYWHERE = ("HD01", "DC02")


def results_of(analysis: Analysis) -> list[Result]:
    modules = list(analysis.program.modules.values())
    found = [*_function_findings(analysis), *_type_findings(analysis), *_file_findings(analysis), *_program_findings(analysis)]
    valid = [allowance for module in modules for allowance in suppress.suppressions_in(module) if not suppress.flaw_of(allowance, analysis.config)]
    hardening = [finding for module in modules for finding in HD01.check(module, found, analysis.config)]
    return [*(Result(finding, _is_silenced(finding, valid)) for finding in found), *(Result(finding, False) for finding in hardening)]


def _is_silenced(finding: Finding, valid: list[Suppression]) -> bool:
    return any(suppress.silences(allowance, finding) for allowance in valid)


def applies(rule: str, module: Module) -> bool:
    """Test rules run on test code, HD01 and DC02 everywhere, every other rule on production code."""
    if rule.startswith(TEST_PREFIX):
        return module.test
    return rule in EVERYWHERE or not module.test


def _function_findings(analysis: Analysis) -> list[Finding]:
    modules = analysis.program.modules
    return [
        finding
        for function in analysis.program.all_functions
        for entry in FUNCTION_RULES
        if applies(entry.rule, modules[function.path])
        for finding in entry.check(function, analysis)
    ]


def _type_findings(analysis: Analysis) -> list[Finding]:
    modules = analysis.program.modules
    return [
        finding
        for info in analysis.program.classes.values()
        for entry in TYPE_RULES
        if applies(entry.rule, modules[info.path])
        for finding in entry.check(info, analysis)
    ]


def _file_findings(analysis: Analysis) -> list[Finding]:
    return [
        finding
        for module in analysis.program.modules.values()
        for entry in FILE_RULES
        if applies(entry.rule, module)
        for finding in entry.check(module, analysis)
    ]


def _program_findings(analysis: Analysis) -> list[Finding]:
    return [finding for entry in PROGRAM_RULES for finding in entry.check(analysis)]
