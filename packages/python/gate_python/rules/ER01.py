"""ER01: A catch block must not be empty and must not only log."""

import ast

from gate_python.findings import on_line
from gate_python.model import Analysis, Finding, Module

RULE = "ER01"
ADVICE = "Handle the error, rethrow it, or let it propagate by removing the handler"
LOG_WORDS = ("log", "logger", "logging", "console", "print", "trace", "debug")


def check(module: Module, analysis: Analysis) -> list[Finding]:
    handlers = [node for node in ast.walk(module.tree) if isinstance(node, ast.ExceptHandler)]
    swallowing = [handler for handler in handlers if all(_is_noise(statement) for statement in handler.body)]
    return [
        on_line(RULE, module.path, handler.lineno, f"except block with {len(handler.body)} statements is empty or only logs. {ADVICE}")
        for handler in swallowing
    ]


def _is_noise(statement: ast.stmt) -> bool:
    """True for pass, a bare literal and a log call: statements that do not handle the error."""
    match statement:
        case ast.Pass() | ast.Expr(value=ast.Constant()):
            return True
        case ast.Expr(value=ast.Call(func=called)):
            return any(word in ast.unparse(called).lower() for word in LOG_WORDS)
    return False
