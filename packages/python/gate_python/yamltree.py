"""Reads YAML text into nodes that remember their file, key and position, so a malformed input can be named exactly."""

from dataclasses import dataclass

import yaml

from gate_python.model import InputError, Problem, SourceFile

BOOLEAN_TAG = "tag:yaml.org,2002:bool"
TRUE_WORDS = ("true", "yes", "on")


@dataclass(frozen=True)
class Entry:
    file: str
    key: str
    node: yaml.Node


def document(source: SourceFile) -> Entry:
    try:
        node = yaml.compose(source.text, Loader=yaml.SafeLoader)
    except yaml.MarkedYAMLError as error:
        raise InputError(_syntax_problem(source.path, error)) from error
    if node is None:
        return Entry(source.path, "", yaml.MappingNode("tag:yaml.org,2002:map", []))
    return Entry(source.path, "", node)


def _syntax_problem(file: str, error: yaml.MarkedYAMLError) -> Problem:
    mark = error.problem_mark
    line, column = (mark.line + 1, mark.column + 1) if mark else (0, 0)
    return Problem(file, line, column, f"not valid YAML, {error.problem}. Correct the YAML syntax at this position")


def failure(entry: Entry, expectation: str) -> InputError:
    mark = entry.node.start_mark
    name = f"key '{entry.key}'" if entry.key else "the document"
    return InputError(Problem(entry.file, mark.line + 1, mark.column + 1, f"{name} {expectation}"))


def child(entry: Entry, key: str) -> Entry | None:
    if not isinstance(entry.node, yaml.MappingNode):
        raise failure(entry, "must be a mapping. Write it as nested 'name: value' lines")
    for name, value in entry.node.value:
        if name.value == key:
            return Entry(entry.file, _joined(entry.key, key), value)
    return None


def required(entry: Entry, key: str) -> Entry:
    found = child(entry, key)
    if found is None:
        raise failure(entry, f"lacks the key '{key}'. Add '{key}' to it")
    return found


def members(entry: Entry | None) -> list[Entry]:
    if entry is None:
        return []
    if isinstance(entry.node, yaml.MappingNode):
        return [Entry(entry.file, _joined(entry.key, name.value), value) for name, value in entry.node.value]
    if isinstance(entry.node, yaml.SequenceNode):
        return [Entry(entry.file, entry.key, item) for item in entry.node.value]
    raise failure(entry, "must be a list or a mapping. Write it as '- item' lines or 'name: value' lines")


def text(entry: Entry) -> str:
    if not isinstance(entry.node, yaml.ScalarNode):
        raise failure(entry, "must be a single value, not a list or mapping. Write one value after the colon")
    return str(entry.node.value)


def texts(entry: Entry | None) -> tuple[str, ...]:
    if entry is not None and not isinstance(entry.node, yaml.SequenceNode):
        raise failure(entry, 'must be a list of strings. Write it as ["a", "b"]')
    return tuple(text(item) for item in members(entry))


def number(entry: Entry) -> int:
    value = text(entry)
    if not value.isdigit():
        raise failure(entry, f"is '{value}' but must be a whole number. Write digits only")
    return int(value)


def flag(entry: Entry | None) -> bool:
    if entry is None:
        return False
    if not isinstance(entry.node, yaml.ScalarNode) or entry.node.tag != BOOLEAN_TAG:
        raise failure(entry, "must be true or false. Write true or false after the colon")
    return text(entry).lower() in TRUE_WORDS


def last_key(entry: Entry) -> str:
    return entry.key.split(".")[-1]


def _joined(prefix: str, key: str) -> str:
    return f"{prefix}.{key}" if prefix else key
