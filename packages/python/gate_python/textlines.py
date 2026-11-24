"""Splits text into lines the way Python's own parser counts them: at \\r\\n, \\r and \\n, and nowhere else.

str.splitlines also breaks at form feeds and at the Unicode separators, which neither the parser nor an editor
treats as the end of a line, so a line number taken from it can point at the wrong line.
"""

import re

BREAK = re.compile(r"\r\n|\r|\n")
BARE_RETURN = re.compile(r"\r(?!\n)")
PIECE = re.compile(r"[^\r\n]*(?:\r\n|\r|\n)|[^\r\n]+")


def lines_of(text: str) -> tuple[str, ...]:
    """The lines without their endings; an ending at the very end of the text opens no further line."""
    parts = BREAK.split(text)
    return tuple(parts[:-1] if parts[-1] == "" else parts)


def pieces_of(text: str) -> list[str]:
    """The lines with their endings, so that joined they are the text again."""
    return PIECE.findall(text)


def with_plain_endings(text: str) -> str:
    """The text with every bare \\r turned into \\n: the same length, and one line per line for the tokenizer."""
    return BARE_RETURN.sub("\n", text)
