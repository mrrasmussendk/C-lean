"""Check of `gate-python mutants`: every mutant of the sample project is applied byte-wise and must parse and differ.

Run: python3 packages/python/checks/check_mutants.py
"""

import ast
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
COMMAND = HERE.parent / "bin" / "gate-python"
SAMPLE = HERE / "samples" / "shop"
MUTATORS = {"ArithmeticOperator", "EqualityOperator", "LogicalOperator", "BooleanLiteral", "UnaryOperator"}
BOM = b"\xef\xbb\xbf"
EXTRA = "# Ünïcode first\r\ndef pick(a: int, b: int) -> bool:\r\n    naïve = 'é' * a\r\n    return a - b >= 0 or not naïve\r\n"


def run_document(root: Path, out: Path, extra: list[str]) -> dict:
    arguments = [str(COMMAND), "mutants", "--root", str(root), "--out", str(out)]
    arguments += ["--config", str(root / "gate.yaml"), "--architecture", str(root / "architecture.yaml"), *extra]
    done = subprocess.run(arguments, capture_output=True, text=True, check=False)
    assert done.returncode == 0, f"gate-python mutants failed: {done.stderr}"
    return json.loads((out / "mutants.json").read_text(encoding="utf-8"))


def run_mutants(root: Path, out: Path, extra: list[str]) -> list[dict]:
    return run_document(root, out, extra)["mutants"]


def verify(root: Path, mutant: dict) -> None:
    stored = (root / mutant["file"]).read_bytes()
    start, end = mutant["start"], mutant["end"]
    assert stored[start:end] == mutant["original"].encode("utf-8"), f"{mutant['id']}: bytes at offsets are not the original"
    before = stored[:start].decode("utf-8-sig" if stored.startswith(BOM) else "utf-8")
    line = before.count("\n") + 1
    column = len(before) - (before.rfind("\n") + 1) + 1
    assert (line, column) == (mutant["line"], mutant["column"]), f"{mutant['id']}: line and column do not locate start"
    assert mutant["id"].startswith(f"{mutant['file']}:{line}:{column}:{mutant['mutator']}"), f"{mutant['id']}: malformed id"
    mutated = stored[:start] + mutant["replacement"].encode("utf-8") + stored[end:]
    assert mutated != stored, f"{mutant['id']}: mutated file does not differ"
    ast.parse(mutated)


def main() -> int:
    with tempfile.TemporaryDirectory() as scratch:
        root = Path(scratch) / "shop"
        shutil.copytree(SAMPLE, root)
        (root / "app" / "extra.py").write_bytes(BOM + EXTRA.encode("utf-8"))
        before = {path: path.read_bytes() for path in root.rglob("*.py")}
        mutants = run_mutants(root, Path(scratch) / "out", [])
        for mutant in mutants:
            verify(root, mutant)
        assert before == {path: path.read_bytes() for path in root.rglob("*.py")}, "mutants changed a source file"
        keys = [(mutant["file"], mutant["start"], mutant["mutator"]) for mutant in mutants]
        assert keys == sorted(keys), "mutants are not sorted by file, start, mutator"
        assert len({mutant["id"] for mutant in mutants}) == len(mutants), "mutant ids are not unique"
        assert {mutant["mutator"] for mutant in mutants} == MUTATORS, "a mutator produced no mutant"
        assert {mutant["context"] for mutant in mutants} == {"", "logging", "guard"}, "a context is missing"
        assert not [mutant for mutant in mutants if mutant["file"] == "test.py"], "test code was mutated"
        skipped = ("FEE = ", "LIMIT = ", "SMALL = ", "LARGE = ", "-> ", "STOCK = ")
        lines = {path: before[root / path].decode("utf-8-sig").split("\n") for path in {mutant["file"] for mutant in mutants}}
        touched = [lines[mutant["file"]][mutant["line"] - 1] for mutant in mutants]
        assert not [text for text in touched if text.strip().startswith(skipped[:4])], "a constant or enum value was mutated"
        assert not [m for m in mutants if m["original"] == "+" and '"é" + name' in lines[m["file"]][m["line"] - 1]], "string concatenation was mutated"
        extra = [mutant for mutant in mutants if mutant["file"] == "app/extra.py"]
        assert sorted(mutant["original"] for mutant in extra) == ["*", "-", ">=", "not", "or"], "BOM, CRLF and non-ASCII file is wrong"
        (root / "lines.yaml").write_text("lines:\n  - {path: app/orders.py, start: 26, end: 30}\n", encoding="utf-8")
        some = run_mutants(root, Path(scratch) / "out", ["--lines", str(root / "lines.yaml")])
        assert some and all(m["file"] == "app/orders.py" and 26 <= m["line"] <= 30 for m in some), "--lines does not filter"
        assert len(some) < len(mutants), "--lines kept everything"
        (root / "lines.yaml").unlink()
        plain = run_document(root, Path(scratch) / "out", [])
        switched = run_document(root, Path(scratch) / "out", ["--switch"])
        assert plain["switched"] is False and switched == plain, "--switch must change nothing and write switched: false"
        assert before == {path: path.read_bytes() for path in root.rglob("*.py")}, "--switch changed a source file"
    print(f"check_mutants: ok, {len(mutants)} mutants applied byte-wise, each parses and differs; --switch changes nothing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
