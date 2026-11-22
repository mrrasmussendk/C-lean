"""Builds findings in the diagnostic format: rule ID, what is wrong, what to do."""

from gate_python.model import ClassInfo, Finding, FunctionInfo


def on_function(rule: str, function: FunctionInfo, detail: str) -> Finding:
    return Finding(rule, function.path, function.node.lineno, f"{rule}: {detail}.", function.symbol)


def on_type(rule: str, info: ClassInfo, detail: str) -> Finding:
    return Finding(rule, info.path, info.node.lineno, f"{rule}: {detail}.", info.symbol)


def on_line(rule: str, path: str, line: int, detail: str) -> Finding:
    return Finding(rule, path, line, f"{rule}: {detail}.", "")
