"""Finds the comments and the source lines of one file from its tokens."""

import ast
import io
import tokenize

from gate_python.model import Comment

LAYOUT = (tokenize.COMMENT, tokenize.NL, tokenize.NEWLINE, tokenize.INDENT, tokenize.DEDENT, tokenize.ENDMARKER)


def tokens_of(text: str) -> list[tokenize.TokenInfo]:
    return list(tokenize.generate_tokens(io.StringIO(text).readline))


def token_lines(tokens: list[tokenize.TokenInfo]) -> frozenset[int]:
    """Lines that hold at least part of a token that is not a comment or layout."""
    code = [token for token in tokens if token.type not in LAYOUT]
    return frozenset(line for token in code for line in range(token.start[0], token.end[0] + 1))


def comments_of(tokens: list[tokenize.TokenInfo]) -> tuple[Comment, ...]:
    occupied = token_lines(tokens)
    found = [token for token in tokens if token.type == tokenize.COMMENT]
    return tuple(Comment(token.start[0], token.string, token.start[0] not in occupied) for token in found)


def code_lines(tokens: list[tokenize.TokenInfo], tree: ast.Module) -> frozenset[int]:
    """Source lines: lines that hold a token other than a comment or a string that stands alone as a statement.

    Such a string does nothing when it runs. It is a documentation string when it comes first in a body and a
    block comment anywhere else. Only the string itself is left out: a line that also holds code is counted,
    so a string cannot be used to hide the code beside it.
    """
    spans = _lone_string_spans(tree)
    return token_lines([token for token in tokens if not (token.type == tokenize.STRING and _within(token, spans))])


Span = tuple[tuple[int, int], tuple[int, int]]


def _lone_string_spans(tree: ast.Module) -> list[Span]:
    """Where each string statement starts and ends, as (line, byte column) pairs."""
    lone = [node for node in ast.walk(tree) if isinstance(node, ast.Expr) and _is_text(node.value)]
    return [
        ((node.lineno, node.col_offset), (node.end_lineno or node.lineno, node.end_col_offset or 0)) for node in lone
    ]


def _within(token: tokenize.TokenInfo, spans: list[Span]) -> bool:
    """Whether the token starts inside one of the spans. The syntax tree counts columns in bytes, tokens in characters."""
    start = (token.start[0], len(token.line[: token.start[1]].encode("utf-8")))
    return any(first <= start < last for first, last in spans)


def _is_text(value: ast.expr) -> bool:
    return isinstance(value, ast.Constant) and isinstance(value.value, str)
