"""AR02: Output parameter: a function must not return data by writing through a parameter. Exempt: the language's standard try-parse pattern."""

import ast

from gate_python import scopes, signature, writes
from gate_python.findings import on_line
from gate_python.model import Analysis, Finding, FunctionInfo
from gate_python.syntax import body_nodes

RULE = "AR02"
ADVICE = "Return the data as the function's result instead"
STATUS_TYPES = ("None", "bool")


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    written = {
        write.parameter for write in writes.parameter_writes(function, scopes.scope_of(function, analysis.program))
    }
    outputs = [parameter for parameter in signature.parameters(function) if parameter.arg in written]
    if not _returns_nothing_or_status(function):
        return []
    return [
        on_line(
            RULE,
            function.path,
            output.lineno,
            f"function {function.name} returns data by writing through parameter {output.arg}. {ADVICE}",
        )
        for output in outputs
    ]


def _returns_nothing_or_status(function: FunctionInfo) -> bool:
    if isinstance(function.node, ast.Lambda):
        return False
    if function.node.returns is not None:
        return ast.unparse(function.node.returns) in STATUS_TYPES
    returned = [node for node in body_nodes(function.node) if isinstance(node, ast.Return)]
    return not any(node.value for node in returned)
