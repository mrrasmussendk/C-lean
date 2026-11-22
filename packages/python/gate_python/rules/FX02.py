"""FX02: Command-query separation: a function that returns a value must not assign to instance, static or module state."""

import ast

from gate_python import scopes, state
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo
from gate_python.syntax import body_nodes

RULE = "FX02"
ADVICE = "Split it into a command that changes state and returns nothing, and a query that returns the value"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    assigned = state.state_writes(function, scopes.scope_of(function, analysis.program))
    if not assigned or not returns_value(function):
        return []
    targets = ", ".join(sorted({ast.unparse(node) for node in assigned}))
    return [on_function(RULE, function, f"function {function.name} returns a value and assigns state ({targets}). {ADVICE}")]


def returns_value(function: FunctionInfo) -> bool:
    returned = [node.value for node in body_nodes(function.node) if isinstance(node, ast.Return) and node.value]
    return any(not (isinstance(value, ast.Constant) and value.value is None) for value in returned)
