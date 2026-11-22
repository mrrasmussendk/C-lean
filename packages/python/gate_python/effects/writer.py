"""File system writes. The only module that writes files."""

import json
from pathlib import Path
from typing import Any, Mapping

from gate_python.model import InputError, Problem

INDENT = 2


def write_documents(directory: str, documents: Mapping[str, Mapping[str, Any]]) -> None:
    """Writes each document as JSON under its file name in the directory."""
    write_texts(directory, {name: json.dumps(document, indent=INDENT) + "\n" for name, document in documents.items()})


def write_texts(directory: str, texts: Mapping[str, str]) -> None:
    """Writes each text as UTF-8, as it is, at its path below the directory."""
    try:
        for name, text in texts.items():
            target = Path(directory).joinpath(name)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(text.encode("utf-8"))
    except OSError as error:
        raise InputError(
            Problem(directory, 0, 0, f"cannot be written, {error.strerror}. Check that the directory is writable")
        ) from error
