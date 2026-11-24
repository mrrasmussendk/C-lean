"""Turns gate.yaml, architecture.yaml and the glossary into the Config the rules read. Pure."""

import re
from dataclasses import dataclass

from gate_python import yamltree
from gate_python.model import Component, Config, InputError, Inputs, Problem, SourceFile
from gate_python.rule_table import BY_ID, LEVELS, OFF, ROWS
from gate_python.yamltree import Entry

DATE = re.compile(r"\d{4}-\d{2}-\d{2}")


def config_of(inputs: Inputs) -> Config:
    if not DATE.fullmatch(inputs.today):
        raise InputError(_date_problem(inputs.today))
    gate = yamltree.document(inputs.config)
    paths = yamltree.child(gate, "paths") or gate
    rules = [_known(entry) for entry in yamltree.members(yamltree.child(gate, "rules"))]
    architecture = yamltree.document(inputs.architecture)
    return Config(
        tests=yamltree.texts(yamltree.child(paths, "tests")),
        generated=yamltree.texts(yamltree.child(paths, "generated")) + _excluded(paths),
        limits={row.rule: row.limit for row in ROWS} | dict(pair for entry in rules for pair in _limit(entry)),
        severities={row.rule: row.severity for row in ROWS}
        | dict(pair for entry in rules for pair in _severity(entry)),
        components=tuple(_component(entry) for entry in yamltree.members(yamltree.child(architecture, "components"))),
        banned=dict(pair for glossary in inputs.glossaries for pair in _banned(glossary)),
        today=inputs.today,
    )


@dataclass(frozen=True)
class LineRange:
    path: str
    start: int
    end: int


def line_ranges(filters: tuple[SourceFile, ...]) -> tuple[LineRange, ...] | None:
    """The line ranges of a --lines file, or None when no file was given and every line is wanted."""
    if not filters:
        return None
    rows = [
        row for source in filters for row in yamltree.members(yamltree.required(yamltree.document(source), "lines"))
    ]
    return tuple(_line_range(row) for row in rows)


def _line_range(row: Entry) -> LineRange:
    path = yamltree.text(yamltree.required(row, "path"))
    return LineRange(
        path, yamltree.number(yamltree.required(row, "start")), yamltree.number(yamltree.required(row, "end"))
    )


def _date_problem(today: str) -> Problem:
    return Problem("", 0, 0, f"--today is '{today}' but must be a date. Pass it as YYYY-MM-DD")


def _known(entry: Entry) -> Entry:
    rule = yamltree.last_key(entry)
    if rule not in BY_ID:
        raise yamltree.failure(
            entry, f"names the rule '{rule}', which spec/rules.yaml does not define. Use a rule ID from the spec"
        )
    return entry


def _excluded(paths: Entry) -> tuple[str, ...]:
    """The folders and files a human left out of the gate, treated as generated code: read, never judged.

    Each entry stands for itself and for everything under it, so legacy, legacy/ and legacy/** mean the same.
    """
    folders = [entry.rstrip("/") for entry in yamltree.texts(yamltree.child(paths, "excluded"))]
    return tuple(pattern for folder in folders for pattern in (folder, f"{folder}/**"))


def _limit(entry: Entry) -> list[tuple[str, int]]:
    limit = yamltree.child(entry, "limit")
    return [(yamltree.last_key(entry), yamltree.number(limit))] if limit else []


def _severity(entry: Entry) -> list[tuple[str, str]]:
    severity = yamltree.child(entry, "severity")
    if severity is None:
        return []
    if yamltree.text(severity) not in (*LEVELS, OFF):
        raise yamltree.failure(
            severity, f"is '{yamltree.text(severity)}' but must be block, warn, report or off. Use one of the four"
        )
    return [(yamltree.last_key(entry), yamltree.text(severity))]


def _component(entry: Entry) -> Component:
    name = yamltree.text(yamltree.required(entry, "name"))
    paths = yamltree.texts(yamltree.required(entry, "paths"))
    return Component(name, paths, yamltree.flag(yamltree.child(entry, "effect")))


def _banned(glossary: SourceFile) -> list[tuple[str, str]]:
    terms = yamltree.members(yamltree.child(yamltree.document(glossary), "terms"))
    return [pair for entry in terms for pair in _synonyms(entry)]


def _synonyms(entry: Entry) -> list[tuple[str, str]]:
    term = yamltree.text(yamltree.required(entry, "term"))
    return [(word.lower(), term) for word in yamltree.texts(yamltree.child(entry, "banned"))]
