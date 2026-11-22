"""gate-corpus: checks one native package against the conformance corpus.

Usage: gate-corpus <language> [suite ...]
Exit 0 when every case, every rule file header and every minimum is satisfied.
"""

import sys
from pathlib import Path

from gate_corpus import parse, run
from gate_corpus.effects import console, files
from gate_corpus.layout import Layout
from gate_corpus.model import Case, Outcome

USAGE_ERROR = 2


def main(arguments: list[str]) -> int:
    if not arguments:
        return USAGE_ERROR
    layout = Layout(Path(__file__).resolve().parents[2], arguments[0])
    suites = _suites(layout, set(arguments[1:]))
    outcomes = [run.run_case(layout, case) for cases in suites.values() for case in cases]
    other = [problem for suite, cases in suites.items() for problem in run.minimum_problems(suite, cases)]
    other += run.header_problems(layout)
    console.report(outcomes, other)
    return int(_failed(outcomes) or bool(other))


def _suites(layout: Layout, wanted: set[str]) -> dict[str, list[Case]]:
    loaded = [parse.cases_of(files.load_yaml(path)) for path in files.suite_files(layout.cases)]
    return {cases[0].suite: cases for cases in loaded if cases and (not wanted or cases[0].suite in wanted)}


def _failed(outcomes: list[Outcome]) -> bool:
    return any(outcome.problems for outcome in outcomes)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
