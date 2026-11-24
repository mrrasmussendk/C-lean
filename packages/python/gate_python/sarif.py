"""Builds findings.sarif (SARIF 2.1.0) from the results."""

import hashlib
from typing import Any

from gate_python.model import Analysis, Finding, Result
from gate_python.rule_table import LEVELS, OFF, PACKAGE_VERSION, TOOL_NAME

SCHEMA = "https://json.schemastore.org/sarif-2.1.0.json"
VERSION = "2.1.0"
FINGERPRINT = "gate/v1"


def document(results: list[Result], analysis: Analysis) -> dict[str, Any]:
    enforced = [result for result in results if analysis.config.severities[result.finding.rule] != OFF]
    ordered = sorted(enforced, key=_order)
    driver = {"name": TOOL_NAME, "version": PACKAGE_VERSION}
    run = {"tool": {"driver": driver}, "results": [_result(result, analysis) for result in ordered]}
    return {"$schema": SCHEMA, "version": VERSION, "runs": [run]}


def _order(result: Result) -> tuple[str, int, str, str]:
    finding = result.finding
    return (finding.file, finding.line, finding.rule, finding.message)


def _result(result: Result, analysis: Analysis) -> dict[str, Any]:
    finding = result.finding
    place = {"artifactLocation": {"uri": finding.file}, "region": {"startLine": finding.line}}
    written = {
        "ruleId": finding.rule,
        "level": LEVELS[analysis.config.severities[finding.rule]],
        "message": {"text": finding.message},
        "locations": [{"physicalLocation": place}],
        "partialFingerprints": {
            FINGERPRINT: f"{finding.rule}|{finding.file}|{finding.anchor or _line_hash(finding, analysis)}"
        },
    }
    return written | {"suppressions": [{"kind": "inSource"}]} if result.suppressed else written


def _line_hash(finding: Finding, analysis: Analysis) -> str:
    """SHA-256 of the trimmed source line the finding is on."""
    texts = {module.path: module.lines for module in analysis.program.modules.values()}
    texts |= {project.path: tuple(project.text.splitlines()) for project in analysis.projects}
    lines = texts.get(finding.file, ())
    line = lines[finding.line - 1] if 0 < finding.line <= len(lines) else ""
    return hashlib.sha256(line.strip().encode("utf-8")).hexdigest()
