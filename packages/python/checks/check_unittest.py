"""Check of `gate-python unittest`: exit codes, the JUnit XML, the file restriction and readable failures.

Run: python3 packages/python/checks/check_unittest.py
"""

import os
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ElementTree
from pathlib import Path

HERE = Path(__file__).resolve().parent
COMMAND = HERE.parent / "bin" / "gate-python"
SAMPLE = HERE / "samples" / "unit"
KEPT = ("PATH", "HOME")


def run_tests(variables: dict[str, str], extra: list[str]) -> subprocess.CompletedProcess:
    environment = {name: os.environ[name] for name in KEPT if name in os.environ} | variables
    return subprocess.run([str(COMMAND), "unittest", *extra], cwd=SAMPLE, env=environment, capture_output=True, text=True, check=False)


def cases(report: Path) -> dict[str, list[str]]:
    """Each testcase as classname.name with the tags of its children."""
    found = ElementTree.parse(report).getroot().iter("testcase")
    return {f"{case.get('classname')}.{case.get('name')}": [child.tag for child in case] for case in found}


def main() -> int:
    with tempfile.TemporaryDirectory() as scratch:
        report = Path(scratch) / "nested" / "junit.xml"
        everything = run_tests({"GATE_JUNIT": str(report)}, ["--start", "tests"])
        assert everything.returncode == 1, f"failing tests must exit 1: {everything.returncode} {everything.stderr}"
        assert cases(report) == {
            "test_calc.AddTests.test_adds": [],
            "test_wrong.WrongTests.test_fails": ["failure"],
            "test_wrong.WrongTests.test_errors": ["error"],
            "unittest.loader._FailedTest.test_broken": ["error"],
        }, f"unexpected testcases {cases(report)}"
        root = ElementTree.parse(report).getroot()
        assert all(float(case.get("time", "x")) >= 0 for case in root.iter("testcase")), "a testcase has no time"
        failure = next(root.iter("failure"))
        assert "3 != 4" in failure.get("message", "") and "Traceback" in (failure.text or ""), "failure lacks message or traceback"
        passing = run_tests({"GATE_JUNIT": str(report), "GATE_TEST_FILES": "tests/test_calc.py"}, ["--start", "tests"])
        assert passing.returncode == 0 and list(cases(report)) == ["test_calc.AddTests.test_adds"], "the file restriction does not hold"
        broken = run_tests({"GATE_JUNIT": str(report), "GATE_TEST_FILES": "tests/test_broken.py"}, ["--start", "tests", "--pattern", "test_b*.py"])
        assert broken.returncode == 1 and cases(report) == {"unittest.loader._FailedTest.test_broken": ["error"]}, "an import error is not a failed testcase"
        unset = run_tests({}, [])
        assert unset.returncode == 2 and unset.stderr.startswith("gate-python: the environment variable GATE_JUNIT is not set"), unset.stderr
        nowhere = run_tests({"GATE_JUNIT": str(report)}, ["--start", "no_such_directory"])
        assert nowhere.returncode == 2 and nowhere.stderr.startswith("gate-python: no_such_directory: test discovery failed"), nowhere.stderr
        assert "Traceback" not in unset.stderr + nowhere.stderr, "a stack trace was printed"
    print("check_unittest: ok, exit codes 1/0/1/2/2, 4 testcases with failure, error and import error, skipped omitted")
    return 0


if __name__ == "__main__":
    sys.exit(main())
