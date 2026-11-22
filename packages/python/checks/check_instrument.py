"""Check of `gate-python instrument`: the instrumented sample behaves as before and traces exactly the expected edges.

Run: python3 packages/python/checks/check_instrument.py
"""

import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
COMMAND = HERE.parent / "bin" / "gate-python"
SAMPLE = HERE / "samples" / "shop"
EXPECTED = {("ui", "app"), ("ui", "store"), ("app", "store"), ("store", "ui")}


def run_script(root: Path) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, "test.py"], cwd=root, capture_output=True, text=True, check=False)


def main() -> int:
    with tempfile.TemporaryDirectory() as scratch:
        root = Path(scratch) / "shop"
        trace = Path(scratch) / "trace.jsonl"
        shutil.copytree(SAMPLE, root)
        plain = run_script(root)
        assert plain.returncode == 0, f"the sample fails before instrumenting: {plain.stderr}"
        test_before = (root / "test.py").read_bytes()
        arguments = [str(COMMAND), "instrument", "--root", str(root), "--trace-file", str(trace)]
        arguments += ["--config", str(root / "gate.yaml"), "--architecture", str(root / "architecture.yaml")]
        done = subprocess.run(arguments, capture_output=True, text=True, check=False)
        assert done.returncode == 0, f"gate-python instrument failed: {done.stderr}"
        assert (root / "test.py").read_bytes() == test_before, "the test file was rewritten"
        assert "_gate_trace.enter('store')" in (root / "store" / "repo.py").read_text(encoding="utf-8"), "store was not rewritten"
        assert "_gate_trace.enter('')" in (root / "misc" / "util.py").read_text(encoding="utf-8"), "the unmapped file was not rewritten"
        traced = run_script(root)
        assert (traced.returncode, traced.stdout, traced.stderr) == (plain.returncode, plain.stdout, plain.stderr), (
            f"behaviour changed:\n{traced.stdout}\n{traced.stderr}"
        )
        again = run_script(root)
        assert again.stdout == plain.stdout, "a second process behaves differently"
        rows = [json.loads(line) for line in trace.read_text(encoding="utf-8").splitlines()]
        assert all(set(row) == {"from", "to"} for row in rows), "a trace line is not {from, to}"
        edges = [(row["from"], row["to"]) for row in rows]
        assert set(edges) == EXPECTED, f"traced edges {sorted(set(edges))}, expected {sorted(EXPECTED)}"
        assert len(edges) == 2 * len(EXPECTED), "each edge must be appended once per process"
    print(f"check_instrument: ok, behaviour unchanged, edges {sorted(EXPECTED)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
