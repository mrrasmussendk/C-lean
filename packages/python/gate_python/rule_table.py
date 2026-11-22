"""The part of spec/rules.yaml this package needs at run time: owner, severity and default limit."""

from dataclasses import dataclass
from typing import Mapping

PACKAGE_VERSION = "0.1.0"
SPEC_VERSION = "1.0"
TOOL_NAME = "gate-python"

NATIVE = "native"
CORE = "core"
BLOCK = "block"
WARN = "warn"
REPORT = "report"
LEVELS: Mapping[str, str] = {BLOCK: "error", WARN: "warning", REPORT: "note"}

NO_LIMIT = 0
SZ01_LIMIT = 30
SZ02_LIMIT = 15
SZ03_LIMIT = 3
SZ04_LIMIT = 4
SZ05_LIMIT = 200
SZ06_LIMIT = 120
DU01_LIMIT = 6
CP01_LIMIT = 1
FR02_LIMIT = 80
FR03_LIMIT = 5
FR04_LIMIT = 6
TS03_LIMIT = 4
DS01_LIMIT = 2
DS02_LIMIT = 2


@dataclass(frozen=True)
class RuleRow:
    rule: str
    owner: str
    severity: str
    limit: int


ROWS: tuple[RuleRow, ...] = (
    RuleRow("SZ01", NATIVE, BLOCK, SZ01_LIMIT),
    RuleRow("SZ02", NATIVE, BLOCK, SZ02_LIMIT),
    RuleRow("SZ03", NATIVE, BLOCK, SZ03_LIMIT),
    RuleRow("SZ04", NATIVE, BLOCK, SZ04_LIMIT),
    RuleRow("SZ05", NATIVE, WARN, SZ05_LIMIT),
    RuleRow("SZ06", NATIVE, REPORT, SZ06_LIMIT),
    RuleRow("AR01", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("AR02", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("FX01", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("FX02", NATIVE, WARN, NO_LIMIT),
    RuleRow("FX03", NATIVE, WARN, NO_LIMIT),
    RuleRow("DU01", CORE, BLOCK, DU01_LIMIT),
    RuleRow("ER01", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("ER02", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("ER03", NATIVE, REPORT, NO_LIMIT),
    RuleRow("CP01", NATIVE, WARN, CP01_LIMIT),
    RuleRow("DC01", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("DC02", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("DC03", NATIVE, WARN, NO_LIMIT),
    RuleRow("FR01", CORE, WARN, NO_LIMIT),
    RuleRow("FR02", NATIVE, WARN, FR02_LIMIT),
    RuleRow("FR03", CORE, WARN, FR03_LIMIT),
    RuleRow("FR04", CORE, REPORT, FR04_LIMIT),
    RuleRow("NM01", NATIVE, WARN, NO_LIMIT),
    RuleRow("TS01", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("TS02", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("TS03", CORE, BLOCK, TS03_LIMIT),
    RuleRow("HD01", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("HD02", CORE, BLOCK, NO_LIMIT),
    RuleRow("HD03", CORE, BLOCK, NO_LIMIT),
    RuleRow("GR01", NATIVE, BLOCK, NO_LIMIT),
    RuleRow("DS01", NATIVE, REPORT, DS01_LIMIT),
    RuleRow("DS02", NATIVE, REPORT, DS02_LIMIT),
)
BY_ID: Mapping[str, RuleRow] = {row.rule: row for row in ROWS}
