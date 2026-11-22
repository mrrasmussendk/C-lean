"""The contract suite: fixture changes run against the core only, and the core's rule table against the spec."""

from pathlib import Path

from gate_corpus.effects import files, process
from gate_corpus.model import Case, Outcome

STOPPING = ("block", "warn")
SUITE = "CONTRACT"


def run(repository: Path) -> list[Outcome]:
    gate = repository / "core" / "bin" / "gate"
    fixtures = files.directories(repository / "corpus" / "contract")
    return [_run_case(gate, fixture) for fixture in fixtures]


def table_problems(repository: Path) -> list[str]:
    gate = repository / "core" / "bin" / "gate"
    rules = files.load_yaml(repository / "spec" / "rules.yaml")["rules"]
    return [problem for rule, row in rules.items() for problem in _explained(gate, rule, str(row["definition"]))]


def _run_case(gate: Path, fixture: Path) -> Outcome:
    case = Case(SUITE, fixture.name, "contract", ())
    expected = files.load_yaml(fixture / "expected.yaml")
    result = process.run([str(gate), "check", "--offline", "--root", str(fixture)])
    return Outcome(case, tuple(_problems(expected, result)), str(fixture))


def _problems(expected: dict[str, object], result: process.Result) -> list[str]:
    stopping = {line.split()[1] for line in result.output.splitlines() if line.split()[:1] and line.split()[0] in STOPPING}
    wanted = {str(rule) for rule in _list(expected["rules"])}
    problems = [] if result.code == expected["exit"] else [f"exit code {result.code}, expected {expected['exit']}"]
    problems += [f"expected a block or warn finding of {rule}, the core reported none" for rule in sorted(wanted - stopping)]
    problems += [f"the core reported a block or warn finding of {rule}, expected none" for rule in sorted(stopping - wanted)]
    return problems


def _list(value: object) -> list[object]:
    return value if isinstance(value, list) else []


def _explained(gate: Path, rule: str, definition: str) -> list[str]:
    result = process.run([str(gate), "explain", rule])
    first = result.output.splitlines()[:1]
    if first == [f"{rule}: {definition}"]:
        return []
    return [f"{rule}: `gate explain` does not open with the spec sentence"]
