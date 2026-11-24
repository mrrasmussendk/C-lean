"""Reads gate:allow suppressions from comments and decides which findings they silence."""

import datetime
import re
from dataclasses import dataclass

from gate_python.model import Config, Finding, Module
from gate_python.rule_table import BY_ID

ALLOW = re.compile(r"gate:allow\b(?:\s+(?P<rule>[A-Z]{2}\d{2})\b)?")
REASON = re.compile(r"(?<![\w-])reason=\"(?P<value>[^\"]+)\"")
EXPIRES = re.compile(r"(?<![\w-])expires=(?P<value>\d{4}-\d{2}-\d{2})(?![\w-])")
APPROVER = re.compile(r"(?<![\w-])approver=(?P<value>[^\s\"]+)")


@dataclass(frozen=True)
class Suppression:
    path: str
    line: int
    covers: int
    rule: str
    reason: str
    expires: str
    approver: str


def suppressions_in(module: Module) -> list[Suppression]:
    marked = [(comment, ALLOW.search(comment.text)) for comment in module.comments]
    return [
        Suppression(
            module.path,
            comment.line,
            comment.line + 1 if comment.alone else comment.line,
            found.group("rule") or "",
            _field(REASON, comment.text),
            _field(EXPIRES, comment.text),
            _field(APPROVER, comment.text),
        )
        for comment, found in marked
        if found
    ]


def _field(pattern: re.Pattern[str], text: str) -> str:
    found = pattern.search(text)
    return found.group("value") if found else ""


def flaw_of(suppression: Suppression, config: Config) -> str:
    """What makes the suppression invalid, or nothing."""
    fields = {
        "a rule ID": suppression.rule,
        "reason": suppression.reason,
        "expires": suppression.expires,
        "approver": suppression.approver,
    }
    missing = [name for name, value in fields.items() if not value]
    if missing:
        return f"suppression lacks {', '.join(missing)}"
    if suppression.rule not in BY_ID:
        return f"suppression names the unknown rule {suppression.rule}"
    if not _is_date(suppression.expires):
        return f"suppression of {suppression.rule} expires on {suppression.expires}, which is not a day that exists"
    if suppression.expires < config.today:
        return f"suppression of {suppression.rule} expired on {suppression.expires}, today is {config.today}"
    return ""


def _is_date(text: str) -> bool:
    """A day that exists: 2027-02-30 and 2027-13-01 have the shape of a date and are none."""
    try:
        datetime.date.fromisoformat(text)
    except ValueError:
        return False
    return True


def silences(suppression: Suppression, finding: Finding) -> bool:
    """A suppression covers one line: its own when that line also holds code, otherwise the line directly below."""
    same_place = finding.file == suppression.path and finding.line == suppression.covers
    return same_place and finding.rule == suppression.rule
