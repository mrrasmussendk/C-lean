"""Splits text into lines the way Python's own parser counts them: at \\r\\n, \\r and \\n, and nowhere else.

str.splitlines also breaks at form feeds and at the Unicode separators, which neither the parser nor an editor
treats as the end of a line, so a line number taken from it can point at the wrong line.
"""

import re

BREAK = re.compile(r"\r\n|\r|\n")
# A \r that ends a line by itself. The tokenizer does not take it for a line ending; the parser does.
BARE_RETURN = re.compile(r"\r(?!\n)")
# One line with its ending, so that the pieces of a text joined are the text again.
PIECE = re.compile(r"[^\r\n]*(?:\r\n|\r|\n)|[^\r\n]+")


def lines_of(text: str) -> tuple[str, ...]:
    """The lines without their endings; an ending at the very end of the text opens no further line."""
    parts = BREAK.split(text)
    return tuple(parts[:-1] if parts[-1] == "" else parts)
