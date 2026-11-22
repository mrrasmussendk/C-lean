"""ER03: A function with side effects must not signal failure through a boolean or integer return value."""

import ast

from gate_python import effect_uses, state
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo
from gate_python.syntax import body_nodes

RULE = "ER03"
ADVICE = "Raise an exception or return a result type that says what failed"


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    effects = [*effect_uses.in_function(function, analysis.program), *state.instance_writes(function)]
    signals = [ast.unparse(node) for node in body_nodes(function.node) if _is_failure_signal(node, function)]
    if not effects or not signals:
        return []
    return [
        on_function(
            RULE,
            function,
            f"function {function.name} has side effects and signals failure with '{signals[0]}'. {ADVICE}",
        )
    ]


def _is_failure_signal(node: ast.AST, function: FunctionInfo) -> bool:
    declared = (
        "" if isinstance(function.node, ast.Lambda) or not function.node.returns else ast.unparse(function.node.returns)
    )
    match (declared, node):
        case ("bool", ast.Return(value=ast.Constant(value=False))):
            return True
        case ("int", ast.Return(value=ast.UnaryOp(op=ast.USub(), operand=ast.Constant()))):
            return True
    return False
