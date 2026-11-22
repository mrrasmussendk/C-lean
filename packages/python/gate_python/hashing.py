"""The normalized hash of a function body or statement sequence (spec steps 1 to 6)."""

import ast
import hashlib
from dataclasses import dataclass
from typing import Sequence

IDENTIFIER = "identifier"
SYNTAX = "syntax"
DECLARED_TYPES = ("annotation", "returns")


@dataclass(frozen=True)
class Token:
    kind: str
    text: str


def normalized_hash(nodes: Sequence[ast.AST]) -> str:
    tokens = [token for node in nodes for token in _tokens(node)]
    names = [token.text for token in tokens if token.kind == IDENTIFIER]
    numbers = {name: index for index, name in enumerate(dict.fromkeys(names))}
    serialized = " ".join(f"${numbers[token.text]}" if token.kind == IDENTIFIER else token.text for token in tokens)
    return hashlib.sha256(serialized.encode("utf-8")).hexdigest()


def _tokens(value: object) -> list[Token]:
    """Identifiers become identifier tokens, literals their type, everything else stays as syntax."""
    match value:
        case ast.Constant(value=literal):
            return [Token(SYNTAX, f"<{type(literal).__name__}>")]
        case ast.expr_context():
            return []
        case ast.AST():
            return [Token(SYNTAX, f"({type(value).__name__}"), *_field_tokens(value), Token(SYNTAX, ")")]
        case list():
            return [Token(SYNTAX, "["), *(token for item in value for token in _tokens(item)), Token(SYNTAX, "]")]
        case str():
            return [Token(IDENTIFIER, value)]
    return [Token(SYNTAX, repr(value))]


def _field_tokens(node: ast.AST) -> list[Token]:
    return [token for name, value in ast.iter_fields(node) for token in _field(name, value)]


def _field(name: str, value: object) -> list[Token]:
    if name in DECLARED_TYPES and isinstance(value, ast.AST):
        return [Token(SYNTAX, f"<type {ast.unparse(value)}>")]
    return _tokens(value)
