"""Runs standard-library unittest discovery and writes JUnit XML. Reads the environment, the clock and test files."""

import os
import sys
import time
import unittest
import xml.etree.ElementTree as ElementTree
from dataclasses import dataclass
from pathlib import Path

from gate_python.model import InputError, Problem

JUNIT_VARIABLE = "GATE_JUNIT"
FILES_VARIABLE = "GATE_TEST_FILES"
PASSED = 0
FAILED = 1
FAILURE = "failure"
ERROR = "error"
IMPORT_FAILURE = "_FailedTest"
SUFFIX = ".py"
PRECISION = 6


@dataclass(frozen=True)
class Outcome:
    """One test: its id, how long it ran, and the kind and traceback of its failure, both empty when it passed."""

    test: str
    seconds: float
    kind: str
    detail: str


class Recorder(unittest.TestResult):
    """A test result that also remembers when each test started and how long it ran."""

    def __init__(self) -> None:
        super().__init__()
        self.started: dict[str, float] = {}
        self.seconds: dict[str, float] = {}

    def startTest(self, test: unittest.TestCase) -> None:
        super().startTest(test)
        self.started[test.id()] = time.perf_counter()

    def stopTest(self, test: unittest.TestCase) -> None:
        self.seconds[test.id()] = time.perf_counter() - self.started[test.id()]
        super().stopTest(test)


def run(start: str, pattern: str) -> int:
    """Discovers and runs the tests, writes the report to $GATE_JUNIT, and returns 0 when all passed, else 1."""
    report = os.environ.get(JUNIT_VARIABLE, "")
    if not report:
        raise InputError(Problem("", 0, 0, f"the environment variable {JUNIT_VARIABLE} is not set. Set it to the path the JUnit XML is written to"))
    wanted = os.environ.get(FILES_VARIABLE, "").split()
    tests = [test for test in _discovered(start, pattern) if not wanted or _is_from(test, wanted)]
    recorder = Recorder()
    unittest.TestSuite(tests).run(recorder)
    _write(Path(report), _document(_outcomes(recorder)))
    return PASSED if recorder.wasSuccessful() else FAILED


def _discovered(start: str, pattern: str) -> list[unittest.TestCase]:
    try:
        return _flat(unittest.defaultTestLoader.discover(start, pattern))
    except (ImportError, OSError) as error:
        raise InputError(Problem(start, 0, 0, f"test discovery failed, {error}. Pass an existing directory to --start")) from error


def _flat(suite: unittest.TestSuite) -> list[unittest.TestCase]:
    found: list[unittest.TestCase] = []
    for item in suite:
        found += _flat(item) if isinstance(item, unittest.TestSuite) else [item]
    return found


def _is_from(test: unittest.TestCase, wanted: list[str]) -> bool:
    """True when the test's module is one of the wanted files; a module that failed to import is matched by name."""
    if type(test).__name__ == IMPORT_FAILURE:
        failed = test.id().rpartition(f"{IMPORT_FAILURE}.")[-1]
        return any(_dotted(name).endswith(failed) or failed.endswith(_dotted(name)) for name in wanted)
    module = sys.modules.get(type(test).__module__)
    origin = module.__file__ if module else None
    return origin is not None and Path(origin).resolve() in [Path(name).resolve() for name in wanted]


def _dotted(name: str) -> str:
    return ".".join(Path(name.removesuffix(SUFFIX)).parts)


def _outcomes(recorder: Recorder) -> list[Outcome]:
    """Every test that ran or failed, in the order it started; skipped tests are left out."""
    failed = {test.id(): (FAILURE, detail) for test, detail in recorder.failures}
    failed |= {test.id(): (ERROR, detail) for test, detail in recorder.errors}
    skipped = {test.id() for test, _ in recorder.skipped}
    names = [name for name in dict.fromkeys([*recorder.seconds, *failed]) if name not in skipped]
    return [Outcome(name, recorder.seconds.get(name, 0.0), *failed.get(name, ("", ""))) for name in names]


def _document(outcomes: list[Outcome]) -> ElementTree.Element:
    counts = {kind: str(len([outcome for outcome in outcomes if outcome.kind == kind])) for kind in (FAILURE, ERROR)}
    suite = ElementTree.Element("testsuite", {"name": "unittest", "tests": str(len(outcomes)), "failures": counts[FAILURE], "errors": counts[ERROR]})
    suite.extend(_case(outcome) for outcome in outcomes)
    return suite


def _case(outcome: Outcome) -> ElementTree.Element:
    owner, _, name = outcome.test.rpartition(".")
    case = ElementTree.Element("testcase", {"classname": owner, "name": name, "time": str(round(outcome.seconds, PRECISION))})
    if outcome.kind:
        problem = ElementTree.Element(outcome.kind, {"message": outcome.detail.strip().splitlines()[-1]})
        problem.text = outcome.detail
        case.append(problem)
    return case


def _write(path: Path, document: ElementTree.Element) -> None:
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        ElementTree.ElementTree(document).write(path, encoding="utf-8", xml_declaration=True)
    except OSError as error:
        raise InputError(Problem(str(path), 0, 0, f"cannot be written, {error.strerror}. Check the path in {JUNIT_VARIABLE}")) from error
