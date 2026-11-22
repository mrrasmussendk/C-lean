"""Check of `gate-python mutants --switch`: every switched mutant behaves as the byte-wise mutated original.

Run: python3 packages/python/checks/check_switch.py
"""

import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
COMMAND = HERE.parent / "bin" / "gate-python"
SAMPLE = HERE / "samples" / "shop"
KEPT = ("PATH", "HOME")
NO_BYTECODE = {"PYTHONDONTWRITEBYTECODE": "1"}
EDGES = {("ui", "app"), ("ui", "store"), ("app", "store"), ("store", "ui")}
UNSWITCHABLE = "store/repo.py:38:16:ArithmeticOperator"


def gate(root: Path, command: list[str]) -> None:
    arguments = [str(COMMAND), *command, "--root", str(root), "--config", str(root / "gate.yaml"), "--architecture", str(root / "architecture.yaml")]
    done = subprocess.run(arguments, capture_output=True, text=True, check=False)
    assert done.returncode == 0, f"gate-python {command[0]} failed: {done.stderr}"


def behaviour(root: Path, variables: dict[str, str]) -> tuple[int, str, str]:
    """Exit code, standard output and the last line of standard error of the sample's test script."""
    environment = {name: os.environ[name] for name in KEPT if name in os.environ} | variables | NO_BYTECODE
    done = subprocess.run([sys.executable, "test.py"], cwd=root, env=environment, capture_output=True, text=True, check=False)
    return (done.returncode, done.stdout, "".join(done.stderr.strip().splitlines()[-1:]))


def mutated(root: Path, mutant: dict) -> tuple[int, str, str]:
    """The behaviour of the original with the mutant applied byte-wise."""
    target = root / mutant["file"]
    stored = target.read_bytes()
    target.write_bytes(stored[: mutant["start"]] + mutant["replacement"].encode("utf-8") + stored[mutant["end"]:])
    try:
        return behaviour(root, {})
    finally:
        target.write_bytes(stored)


def main() -> int:
    with tempfile.TemporaryDirectory() as scratch:
        original, switched = Path(scratch) / "original" / "shop", Path(scratch) / "switched" / "shop"
        shutil.copytree(SAMPLE, original)
        shutil.copytree(SAMPLE, switched)
        baseline = behaviour(original, {})
        assert baseline[0] == 0, f"the sample fails: {baseline}"
        gate(switched, ["mutants", "--switch", "--out", str(Path(scratch) / "out")])
        document = json.loads((Path(scratch) / "out" / "mutants.json").read_text(encoding="utf-8"))
        assert document["switched"] is True, "switched must be true"
        backed_out = [mutant["id"] for mutant in document["mutants"] if mutant.get("status") == "CompileError"]
        assert backed_out == [UNSWITCHABLE], f"exactly the mutant in a match pattern must be backed out, got {backed_out}"
        live = [mutant for mutant in document["mutants"] if "status" not in mutant]
        reached_file = Path(scratch) / "reached.txt"
        assert behaviour(switched, {"GATE_MUTANT_REACHED": str(reached_file)}) == baseline, "switched code with no mutant on differs from the original"
        assert behaviour(switched, {"GATE_MUTANT": "no such mutant"}) == baseline, "an unknown mutant id changes behaviour"
        reached = reached_file.read_text(encoding="utf-8").splitlines()
        uncalled = {mutant["id"] for mutant in live if mutant["file"] == "app/orders.py" and mutant["line"] >= 79}
        assert len(uncalled) == 4 and len(reached) == len(set(reached)), "the uncalled function must hold 4 mutants; ids are reached once"
        assert set(reached) == {mutant["id"] for mutant in live} - uncalled, f"reached {sorted(reached)}"
        results = {mutant["id"]: behaviour(switched, {"GATE_MUTANT": mutant["id"]}) for mutant in live}
        for mutant in live:
            assert results[mutant["id"]] == mutated(original, mutant), f"{mutant['id']}: switched and byte-wise mutated behaviour differ"
        killed = [name for name, result in results.items() if result != baseline]
        assert killed and all(results[name] == baseline for name in uncalled), "no mutant changes behaviour, or an unreached one does"
        trace = Path(scratch) / "trace.jsonl"
        gate(switched, ["instrument", "--trace-file", str(trace)])
        assert behaviour(switched, {}) == baseline, "instrumenting the switched tree changes behaviour"
        edges = {(row["from"], row["to"]) for row in map(json.loads, trace.read_text(encoding="utf-8").splitlines())}
        assert edges == EDGES, f"traced edges {sorted(edges)}"
        assert behaviour(switched, {"GATE_MUTANT": killed[0]})[:2] == results[killed[0]][:2], "a switch stopped working after instrumenting"
    print(f"check_switch: ok, {len(live)} mutants switched and equal to byte-wise mutation, {len(killed)} change behaviour, 1 backed out, {len(uncalled)} unreached, instrument on top works")
    return 0


if __name__ == "__main__":
    sys.exit(main())
