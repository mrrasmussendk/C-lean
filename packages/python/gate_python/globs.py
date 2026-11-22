"""Path globs of gate.yaml and architecture.yaml: ** is any number of directories, * stays in one segment."""

import re

WILDCARDS = re.compile(r"(\*\*/|\*\*|\*)")


def matches_any(path: str, patterns: tuple[str, ...]) -> bool:
    return any(re.fullmatch(_expression(pattern), path) for pattern in patterns)


def _expression(pattern: str) -> str:
    return "".join(_piece(part) for part in WILDCARDS.split(pattern))


def _piece(part: str) -> str:
    match part:
        case "**/":
            return "(?:.*/)?"
        case "**":
            return ".*"
        case "*":
            return "[^/]*"
    return re.escape(part)
