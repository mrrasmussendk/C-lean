"""Finds the comments and the source lines of one file from its tokens."""

import ast
import io
import tokenize

from gate_python.model import Comment
from gate_python.syntax import has_docstring

LAYOUT = (tokenize.COMMENT, tokenize.NL, tokenize.NEWLINE, tokenize.INDENT, tokenize.DEDENT, tokenize.ENDMARKER)
DOCUMENTED = (ast.Module, ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)


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
    """Source lines: neither blank, nor comment-only, nor part of a documentation string."""
    return token_lines(tokens) - _docstring_lines(tree)


def _docstring_lines(tree: ast.Module) -> frozenset[int]:
    documented = [node for node in ast.walk(tree) if isinstance(node, DOCUMENTED) and has_docstring(node.body)]
    spans = [(node.body[0].lineno, node.body[0].end_lineno or node.body[0].lineno) for node in documented]
    return frozenset(line for first, last in spans for line in range(first, last + 1))
