"""Reads gate:allow suppressions from comments and decides which findings they silence."""

import re
from dataclasses import dataclass

from gate_python.model import Config, Finding, Module
from gate_python.rule_table import BY_ID

ALLOW = re.compile(r"gate:allow\b(?:\s+(?P<rule>[A-Z]{2}\d{2})\b)?")
REASON = re.compile(r"reason=\"(?P<value>[^\"]+)\"")
EXPIRES = re.compile(r"expires=(?P<value>\d{4}-\d{2}-\d{2})")
APPROVER = re.compile(r"approver=(?P<value>[^\s\"]+)")


@dataclass(frozen=True)
class Suppression:
    path: str
    line: int
    rule: str
    reason: str
    expires: str
    approver: str


def suppressions_in(module: Module) -> list[Suppression]:
    marked = [(comment, ALLOW.search(comment.text)) for comment in module.comments]
    return [
        Suppression(module.path, comment.line, found.group("rule") or "", _field(REASON, comment.text), _field(EXPIRES, comment.text), _field(APPROVER, comment.text))
        for comment, found in marked
        if found
    ]


def _field(pattern: re.Pattern[str], text: str) -> str:
    found = pattern.search(text)
    return found.group("value") if found else ""


def flaw_of(suppression: Suppression, config: Config) -> str:
    """What makes the suppression invalid, or nothing."""
    fields = {"a rule ID": suppression.rule, "reason": suppression.reason, "expires": suppression.expires, "approver": suppression.approver}
    missing = [name for name, value in fields.items() if not value]
    if missing:
        return f"suppression lacks {', '.join(missing)}"
    if suppression.rule not in BY_ID:
        return f"suppression names the unknown rule {suppression.rule}"
    if suppression.expires < config.today:
        return f"suppression of {suppression.rule} expired on {suppression.expires}, today is {config.today}"
    return ""


def silences(suppression: Suppression, finding: Finding) -> bool:
    """A suppression covers its own line and the line directly below."""
    same_place = finding.file == suppression.path and finding.line in (suppression.line, suppression.line + 1)
    return same_place and finding.rule == suppression.rule
