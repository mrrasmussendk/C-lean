"""The rules this package decides, in explicit lists by what each one examines."""

from dataclasses import dataclass
from typing import Callable

from gate_python.model import Analysis, ClassInfo, Finding, FunctionInfo, Module
from gate_python.rules import (
    AR01,
    AR02,
    CP01,
    DC01,
    DC02,
    DC03,
    ER01,
    ER02,
    ER03,
    FR02,
    FX01,
    FX02,
    FX03,
    GR01,
    NM01,
    SZ01,
    SZ02,
    SZ03,
    SZ04,
    SZ05,
    SZ06,
    TS01,
    TS02,
)


@dataclass(frozen=True)
class FunctionRule:
    rule: str
    check: Callable[[FunctionInfo, Analysis], list[Finding]]


@dataclass(frozen=True)
class TypeRule:
    rule: str
    check: Callable[[ClassInfo, Analysis], list[Finding]]


@dataclass(frozen=True)
class FileRule:
    rule: str
    check: Callable[[Module, Analysis], list[Finding]]


@dataclass(frozen=True)
class ProgramRule:
    rule: str
    check: Callable[[Analysis], list[Finding]]


FUNCTION_RULES: tuple[FunctionRule, ...] = (
    FunctionRule(SZ01.RULE, SZ01.check),
    FunctionRule(SZ02.RULE, SZ02.check),
    FunctionRule(SZ03.RULE, SZ03.check),
    FunctionRule(SZ04.RULE, SZ04.check),
    FunctionRule(AR01.RULE, AR01.check),
    FunctionRule(AR02.RULE, AR02.check),
    FunctionRule(FX02.RULE, FX02.check),
    FunctionRule(FX03.RULE, FX03.check),
    FunctionRule(ER03.RULE, ER03.check),
    FunctionRule(FR02.RULE, FR02.check),
    FunctionRule(TS01.RULE, TS01.check),
    FunctionRule(TS02.RULE, TS02.check),
)
TYPE_RULES: tuple[TypeRule, ...] = (TypeRule(SZ05.RULE, SZ05.check),)
FILE_RULES: tuple[FileRule, ...] = (
    FileRule(FX01.RULE, FX01.check),
    FileRule(ER01.RULE, ER01.check),
    FileRule(CP01.RULE, CP01.check),
    FileRule(DC02.RULE, DC02.check),
    FileRule(DC03.RULE, DC03.check),
    FileRule(NM01.RULE, NM01.check),
    FileRule(GR01.RULE, GR01.check),
    FileRule(SZ06.RULE, SZ06.check),
)
PROGRAM_RULES: tuple[ProgramRule, ...] = (ProgramRule(ER02.RULE, ER02.check), ProgramRule(DC01.RULE, DC01.check))
