"""File system reads. The only module that reads files."""

import io
import tokenize
from pathlib import Path

from gate_python.model import InputError, Inputs, Options, Problem, SourceFile

SOURCES = "*.py"
PROJECT_FILE = "pyproject.toml"
GLOSSARY = ".gate/glossary.yaml"
IGNORE = ".c-leanignore"
CACHE = "__pycache__"
HIDDEN = "."
RUNTIME = "runtime"
TRACE_HELPER = "gate_trace.py"
MUTANT_HELPER = "gate_mutant.py"
ENCODING = "utf-8-sig"
UTF8 = "utf-8"
RECODE = "Save it as UTF-8, or name its encoding in a coding line at the top (# -*- coding: <name> -*-)"


def load(options: Options) -> Inputs:
    root = Path(options.root)
    if not root.is_dir():
        raise InputError(
            Problem(options.root, 0, 0, "--root is not a directory. Pass the directory that holds the sources")
        )
    glossary = root / GLOSSARY
    sources = [_source(root / path, path.as_posix()) for path in _paths_under(root, SOURCES)]
    ignore = Path(options.config).resolve().parent / IGNORE
    return Inputs(
        sources=tuple(item for item in sources if isinstance(item, SourceFile)),
        config=_file(Path(options.config), options.config),
        architecture=_file(Path(options.architecture), options.architecture),
        ignores=(_file(ignore, IGNORE),) if ignore.is_file() else (),
        glossaries=(_file(glossary, GLOSSARY),) if glossary.is_file() else (),
        projects=tuple(_file(root / path, path.as_posix()) for path in _paths_under(root, PROJECT_FILE)),
        today=options.today,
        root=str(root.resolve()),
        line_filters=(_file(Path(options.lines), options.lines),) if options.lines else (),
        helper=_helper(TRACE_HELPER),
        mutant_helper=_helper(MUTANT_HELPER),
        unreadable=tuple(item for item in sources if isinstance(item, Problem)),
    )


def _helper(name: str) -> str:
    """The source of a runtime helper that ships inside this package."""
    return _file(Path(__file__).resolve().parent.parent / RUNTIME / name, f"the runtime helper {name}").text


def _paths_under(root: Path, pattern: str) -> list[Path]:
    """The matching files below root, by root-relative path, without hidden and cache directories."""
    relative = sorted(path.relative_to(root) for path in root.rglob(pattern) if path.is_file())
    return [path for path in relative if not any(part == CACHE or part.startswith(HIDDEN) for part in path.parts)]


def _source(path: Path, shown: str) -> SourceFile | Problem:
    """A Python source in the encoding it declares (PEP 263), UTF-8 without a declaration.

    A file whose bytes are not text in that encoding is a problem of that file alone, like one that does not parse.
    """
    try:
        stored = path.read_bytes()
        declared, _ = tokenize.detect_encoding(io.BytesIO(stored).readline)
        text = stored.decode(declared)
    except OSError as error:
        raise InputError(Problem(shown, 0, 0, f"cannot be read, {_reason(error)}. Check that the file exists")) from error
    except (SyntaxError, UnicodeDecodeError, LookupError):
        return Problem(shown, 0, 0, f"cannot be read as text. {RECODE}")
    encoding = UTF8 if declared == ENCODING else declared
    return SourceFile(shown, text, len(stored) - len(text.encode(encoding)), encoding)


def _file(path: Path, shown: str) -> SourceFile:
    """The file's text with line endings as stored, and the size of a byte-order mark in front of it."""
    try:
        stored = path.read_bytes()
        text = stored.decode(ENCODING)
    except (OSError, UnicodeDecodeError) as error:
        raise InputError(
            Problem(shown, 0, 0, f"cannot be read, {_reason(error)}. Check that the file exists and is UTF-8 text")
        ) from error
    return SourceFile(shown, text, len(stored) - len(text.encode("utf-8")))


def _reason(error: Exception) -> str:
    return error.strerror or "unknown error" if isinstance(error, OSError) else "it is not UTF-8"
