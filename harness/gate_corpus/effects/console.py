"""Console output for the harness. The only module that prints."""

from gate_corpus.model import Outcome


def report(outcomes: list[Outcome], other_problems: list[str]) -> None:
    for outcome in outcomes:
        for problem in outcome.problems:
            print(f"FAIL {outcome.case.suite}/{outcome.case.case_id}: {problem}")
    for problem in other_problems:
        print(f"FAIL {problem}")
    failed = sum(1 for outcome in outcomes if outcome.problems)
    print(f"{len(outcomes) - failed} of {len(outcomes)} cases pass, {len(other_problems)} other problems")
