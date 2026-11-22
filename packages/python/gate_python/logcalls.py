"""What a log call is: its receiver or function name is, or contains, one of the log words."""

import ast

LOG_WORDS = ("log", "logger", "logging", "console", "print", "trace", "debug")


def is_log_call(called: ast.expr) -> bool:
    return any(word in ast.unparse(called).lower() for word in LOG_WORDS)
