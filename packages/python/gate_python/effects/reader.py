"""File system reads. The only module that reads files."""

from pathlib import Path

from gate_python.model import InputError, Inputs, Options, Problem, SourceFile

SOURCES = "*.py"
PROJECT_FILE = "pyproject.toml"
GLOSSARY = ".gate/glossary.yaml"
CACHE = "__pycache__"
HIDDEN = "."


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
    )


def _under(root: Path, pattern: str) -> tuple[SourceFile, ...]:
    """The matching files below root, by root-relative path, without hidden and cache directories."""
    relative = sorted(path.relative_to(root) for path in root.rglob(pattern) if path.is_file())
    visible = [path for path in relative if not any(part == CACHE or part.startswith(HIDDEN) for part in path.parts)]
    return tuple(_file(root / path, path.as_posix()) for path in visible)


def _file(path: Path, shown: str) -> SourceFile:
    try:
        return SourceFile(shown, path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError) as error:
        raise InputError(Problem(shown, 0, 0, f"cannot be read, {_reason(error)}. Check that the file exists and is UTF-8 text")) from error


def _reason(error: Exception) -> str:
    return error.strerror or "unknown error" if isinstance(error, OSError) else "it is not UTF-8"
