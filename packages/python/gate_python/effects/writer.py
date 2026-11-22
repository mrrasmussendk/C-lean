"""File system writes. The only module that writes files."""

import json
from pathlib import Path
from typing import Any, Mapping

from gate_python.model import InputError, Outputs, Problem

INDENT = 2


def write(out: str, outputs: Outputs) -> None:
    documents = {"findings.sarif": outputs.sarif, "graph.json": outputs.graph, "units.json": outputs.units}
    try:
        Path(out).mkdir(parents=True, exist_ok=True)
        for name, document in documents.items():
            _write_json(Path(out) / name, document)
    except OSError as error:
        raise InputError(Problem(out, 0, 0, f"cannot be written, {error.strerror}. Check that --out is a writable directory")) from error


def _write_json(path: Path, document: Mapping[str, Any]) -> None:
    path.write_text(json.dumps(document, indent=INDENT) + "\n", encoding="utf-8")
