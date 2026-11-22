"""File system writes. The only module that writes files."""

import json
from pathlib import Path

from gate_python.model import InputError, Outputs, Problem

INDENT = 2


def write(directory: str, outputs: Outputs) -> None:
    """Writes the documents as JSON and the texts as they are, at their paths below the directory."""
    texts = {name: json.dumps(document, indent=INDENT) + "\n" for name, document in outputs.documents.items()} | dict(outputs.texts)
    try:
        Path(directory).mkdir(parents=True, exist_ok=True)
        for name, text in texts.items():
            Path(directory).joinpath(name).write_bytes(text.encode("utf-8"))
    except OSError as error:
        raise InputError(Problem(directory, 0, 0, f"cannot be written, {error.strerror}. Check that the directory is writable")) from error
