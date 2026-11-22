"""ER01: A catch block must not be empty and must not only log."""

import ast

from gate_python.findings import on_line
from gate_python.logcalls import is_log_call
from gate_python.model import Analysis, Finding, Module

RULE = "ER01"
ADVICE = "Handle the error, rethrow it, or let it propagate by removing the handler"


def check(module: Module, analysis: Analysis) -> list[Finding]:
    handlers = [node for node in ast.walk(module.tree) if isinstance(node, ast.ExceptHandler)]
    swallowing = [handler for handler in handlers if all(_is_noise(statement) for statement in handler.body)]
    return [
        on_line(
            RULE,
            module.path,
            handler.lineno,
            f"except block with {len(handler.body)} statements is empty or only logs. {ADVICE}",
        )
        for handler in swallowing
    ]


def _is_noise(statement: ast.stmt) -> bool:
    """True for a statement that does not handle the error: a log call, a literal, a value-less control transfer."""
    match statement:
        case ast.Pass() | ast.Continue() | ast.Break() | ast.Expr(value=ast.Constant()):
            return True
        case ast.Return(value=None) | ast.Return(value=ast.Constant(value=None)):
            return True
        case ast.Expr(value=ast.Call(func=called)):
            return is_log_call(called)
    return False
