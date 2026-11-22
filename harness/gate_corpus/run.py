"""Runs corpus cases for one language and collects what does not match."""

from pathlib import Path

from gate_corpus import compare, parse
from gate_corpus.effects import files, process
from gate_corpus.layout import Layout
from gate_corpus.model import Case, Outcome

SUITE_KINDS = ("equal", "different", "graph")


def run_case(layout: Layout, case: Case) -> Outcome:
    fixture = layout.fixture(case)
    if not files.exists(fixture / "expected.yaml"):
        return Outcome(case, ("no fixture",))
    out = files.fresh_directory(layout.out(case))
    failure = process.analyze(_analysis(layout, fixture), fixture, out)
    if failure:
        return Outcome(case, (failure,))
    return Outcome(case, tuple(_problems(case, fixture, out)))


def _analysis(layout: Layout, fixture: Path) -> process.Analysis:
    config = _own_or_default(fixture, layout.defaults, "gate.yaml")
    architecture = _own_or_default(fixture, layout.defaults, "architecture.yaml")
    return process.Analysis(layout.command, config, architecture)


def _own_or_default(fixture: Path, defaults: Path, name: str) -> Path:
    own = fixture / name
    return own if files.exists(own) else defaults / name


def _problems(case: Case, fixture: Path, out: Path) -> list[str]:
    expected = files.load_yaml(fixture / "expected.yaml")
    if case.kind == "graph":
        return compare.graph_problems(parse.graph_of(expected), parse.graph_of(files.load_json(out / "graph.json")))
    if case.kind in SUITE_KINDS:
        units = files.load_json(out / "units.json")
        left, right = parse.hashes_named(units, "left"), parse.hashes_named(units, "right")
        return compare.hash_problems(bool(expected["equal"]), left, right)
    actual = parse.sarif_findings(files.load_json(out / "findings.sarif"))
    return compare.finding_problems(case, parse.expected_findings(expected), actual)


def header_problems(layout: Layout) -> list[str]:
    rules = files.load_yaml(layout.rules)["rules"]
    native = [rule for rule, row in rules.items() if _applies(row, layout.language)]
    return [problem for rule in native for problem in _header(layout, rule, str(rules[rule]["definition"]))]


def _applies(row: dict[str, object], language: str) -> bool:
    languages = row.get("languages", [language])
    return row["owner"] == "native" and isinstance(languages, list) and language in languages


def _header(layout: Layout, rule: str, definition: str) -> list[str]:
    found = files.rule_files(layout.package, rule)
    if len(found) != 1:
        return [f"{rule}: expected one rule file named {rule}.*, found {len(found)}"]
    return compare.header_problems(rule, definition, files.first_lines(found[0]))


def minimum_problems(suite: str, cases: list[Case]) -> list[str]:
    if any(case.kind in SUITE_KINDS for case in cases):
        return []
    return compare.minimum_problems(suite, cases)
