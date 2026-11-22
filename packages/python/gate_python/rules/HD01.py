"""HD01: An inline suppression must name the rule ID, a reason, an expiry date and an approver."""

import re
from typing import Sequence

from gate_python import suppress
from gate_python.findings import on_line
from gate_python.model import Config, Finding, Module
from gate_python.rule_table import BY_ID, NATIVE
from gate_python.suppress import Suppression

RULE = "HD01"
ADVICE = 'Write gate:allow <RULE-ID> reason="<text>" expires=<YYYY-MM-DD> approver=<name>, or remove it'
SWITCH = re.compile(r"#\s*noqa\b|#\s*type:\s*ignore|#\s*pyright:\s*ignore")


def check(module: Module, findings: Sequence[Finding], config: Config) -> list[Finding]:
    allowances = suppress.suppressions_in(module)
    flawed = [
        (allowance.line, suppress.flaw_of(allowance, config) or _idle(allowance, findings)) for allowance in allowances
    ]
    switches = [
        (comment.line, "a compiler or linter disable switch is not an accepted suppression")
        for comment in module.comments
        if SWITCH.search(comment.text)
    ]
    return [on_line(RULE, module.path, line, f"{flaw}. {ADVICE}") for line, flaw in [*flawed, *switches] if flaw]


def _idle(allowance: Suppression, findings: Sequence[Finding]) -> str:
    """Says so when a valid suppression of a rule this package decides silences no finding."""
    if BY_ID[allowance.rule].owner != NATIVE or any(suppress.silences(allowance, finding) for finding in findings):
        return ""
    return f"suppression of {allowance.rule} silences nothing on its line or the line below"
