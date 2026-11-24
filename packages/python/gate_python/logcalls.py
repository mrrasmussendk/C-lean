"""What a log call is: a word of its receiver or of its function name is one of the log words.

Words are the parts of an identifier in snake_case or camelCase, compared without case: logger.info,
self._log.debug and print_error are log calls, catalog.restore and dialog.close are not.
"""

import ast
import re

LOG_WORDS = ("log", "logger", "logging", "console", "print", "trace", "debug")
WORD = re.compile(r"[A-Z]+(?![a-z])|[A-Z]?[a-z]+")


def is_log_call(called: ast.expr) -> bool:
    words = {word.lower() for identifier in _identifiers(called) for word in WORD.findall(identifier)}
    return any(word in words for word in LOG_WORDS)


def _identifiers(called: ast.expr) -> list[str]:
    """The names the called expression is made of: its variables and the members it reaches through them."""
    nodes = list(ast.walk(called))
    names = [node.id for node in nodes if isinstance(node, ast.Name)]
    return [*names, *(node.attr for node in nodes if isinstance(node, ast.Attribute))]
