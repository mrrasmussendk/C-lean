"""NM01: Glossary: an identifier must not use a word the glossary lists as a banned synonym of a domain term."""

import ast
import re
from typing import Mapping

from gate_python.findings import on_line
from gate_python import scopes, signature
from gate_python.model import Analysis, Finding, Module, Program
from gate_python.syntax import DEFINITIONS, body_nodes

RULE = "NM01"
WORD = re.compile(r"[A-Z]+(?![a-z])|[A-Z]?[a-z]+")
PLURALS = ("", "s", "es")


def check(module: Module, analysis: Analysis) -> list[Finding]:
    declared = [pair for group in _groups(module, analysis.program) for pair in _first_declarations(group)]
    banned = [(name, line, word) for name, line in declared for word in _banned_words(name, analysis.config.banned)]
    return [
        on_line(
            RULE,
            module.path,
            line,
            _detail(name, word, analysis.config.banned[word]),
        )
        for name, line, word in banned
    ]


def _groups(module: Module, program: Program) -> list[list[ast.AST]]:
    """The nodes of each scope: the top level with the class bodies, and every function with its parameters."""
    top = [item.node for item in scopes.top_nodes(module, program)]
    functions = scopes.functions_in(module, program)
    return [top, *([*signature.parameters(function), *body_nodes(function.node)] for function in functions)]


def _first_declarations(nodes: list[ast.AST]) -> list[tuple[str, int]]:
    declared = [pair for node in nodes for pair in _declaration(node)]
    return list(dict(reversed(declared)).items())


def _declaration(node: ast.AST) -> list[tuple[str, int]]:
    """The identifier a node declares, with its line: definitions, parameters and assigned names."""
    if isinstance(node, DEFINITIONS):
        return [(node.name, node.lineno)]
    match node:
        case ast.arg(arg=name) | ast.Name(id=name, ctx=ast.Store()) | ast.Attribute(attr=name, ctx=ast.Store()):
            return [(name, node.lineno)]
    return []


def _banned_words(identifier: str, banned: Mapping[str, str]) -> list[str]:
    words = [word.lower() for word in WORD.findall(identifier)]
    return [synonym for synonym in banned if any(synonym + plural in words for plural in PLURALS)]


def _detail(identifier: str, word: str, term: str) -> str:
    return f"identifier {identifier} uses '{word}', a banned synonym of '{term}'. Rename it to use '{term}'"
