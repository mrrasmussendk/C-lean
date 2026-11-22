"""File system reads. The only module that reads files."""

from pathlib import Path

from gate_python.model import InputError, Inputs, Options, Problem, SourceFile

SOURCES = "*.py"
PROJECT_FILE = "pyproject.toml"
GLOSSARY = ".gate/glossary.yaml"
CACHE = "__pycache__"
HIDDEN = "."
RUNTIME = "runtime"
TRACE_HELPER = "gate_trace.py"
MUTANT_HELPER = "gate_mutant.py"
ENCODING = "utf-8-sig"


def load(options: Options) -> Inputs:
    root = Path(options.root)
    if not root.is_dir():
        raise InputError(Problem(options.root, 0, 0, "--root is not a directory. Pass the directory that holds the sources"))
    glossary = root / GLOSSARY
    return Inputs(
        sources=_under(root, SOURCES),
        config=_file(Path(options.config), options.config),
        architecture=_file(Path(options.architecture), options.architecture),
        glossaries=(_file(glossary, GLOSSARY),) if glossary.is_file() else (),
        projects=_under(root, PROJECT_FILE),
        today=options.today,
        root=str(root.resolve()),
        line_filters=(_file(Path(options.lines), options.lines),) if options.lines else (),
        helper=_helper(TRACE_HELPER),
        mutant_helper=_helper(MUTANT_HELPER),
    )


def _helper(name: str) -> str:
    """The source of a runtime helper that ships inside this package."""
    return _file(Path(__file__).resolve().parent.parent / RUNTIME / name, f"the runtime helper {name}").text


def _under(root: Path, pattern: str) -> tuple[SourceFile, ...]:
    """The matching files below root, by root-relative path, without hidden and cache directories."""
    relative = sorted(path.relative_to(root) for path in root.rglob(pattern) if path.is_file())
    visible = [path for path in relative if not any(part == CACHE or part.startswith(HIDDEN) for part in path.parts)]
    return tuple(_file(root / path, path.as_posix()) for path in visible)


def _file(path: Path, shown: str) -> SourceFile:
    """The file's text with line endings as stored, and the size of a byte-order mark in front of it."""
    try:
        stored = path.read_bytes()
        text = stored.decode(ENCODING)
    except (OSError, UnicodeDecodeError) as error:
        raise InputError(Problem(shown, 0, 0, f"cannot be read, {_reason(error)}. Check that the file exists and is UTF-8 text")) from error
    return SourceFile(shown, text, len(stored) - len(text.encode("utf-8")))


def _reason(error: Exception) -> str:
    return error.strerror or "unknown error" if isinstance(error, OSError) else "it is not UTF-8"
