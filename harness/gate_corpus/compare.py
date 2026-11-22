"""Pure comparison of what a native package produced with what a case expects."""

from gate_corpus.model import Case, Finding, Graph

MINIMUM_VIOLATING = 3
MINIMUM_CONFORMING = 3
MINIMUM_EDGE = 2
REQUIRED_KINDS = {"violating": MINIMUM_VIOLATING, "conforming": MINIMUM_CONFORMING, "edge": MINIMUM_EDGE}


def finding_problems(case: Case, expected: frozenset[Finding], actual: frozenset[Finding]) -> list[str]:
    relevant = frozenset(finding for finding in actual if finding.rule in case.rules)
    missing = [f"expected {_show(finding)}, the package reported none there" for finding in sorted(expected - relevant, key=_show)]
    extra = [f"the package reported {_show(finding)}, expected none there" for finding in sorted(relevant - expected, key=_show)]
    return missing + extra


def _show(finding: Finding) -> str:
    return f"{finding.rule} at {finding.file}:{finding.line}"


def hash_problems(expect_equal: bool, left: list[str], right: list[str]) -> list[str]:
    if len(left) != 1 or len(right) != 1:
        return [f"expected one unit named left and one named right, found {len(left)} and {len(right)}"]
    if (left[0] == right[0]) == expect_equal:
        return []
    return ["hashes are equal but must differ" if left[0] == right[0] else "hashes differ but must be equal"]


def graph_problems(expected: Graph, actual: Graph) -> list[str]:
    problems = [f"missing node {node.symbol} ({node.kind}, {node.component})" for node in expected.nodes - actual.nodes]
    problems += [f"unexpected node {node.symbol} ({node.kind}, {node.component})" for node in actual.nodes - expected.nodes]
    problems += [f"missing edge {edge.source} -{edge.kind}-> {edge.target}" for edge in expected.edges - actual.edges]
    problems += [f"unexpected edge {edge.source} -{edge.kind}-> {edge.target}" for edge in actual.edges - expected.edges]
    return sorted(problems)


def minimum_problems(suite: str, cases: list[Case]) -> list[str]:
    counts = {kind: sum(1 for case in cases if case.kind == kind) for kind in REQUIRED_KINDS}
    return [
        f"{suite}: {counts[kind]} {kind} cases, minimum {required}"
        for kind, required in REQUIRED_KINDS.items()
        if counts[kind] < required
    ]


def header_problems(rule: str, definition: str, first_lines: list[str]) -> list[str]:
    wanted = f"{rule}: {definition}"
    if any(wanted in line for line in first_lines):
        return []
    return [f"{rule}: rule file does not open with the spec sentence '{wanted}'"]
