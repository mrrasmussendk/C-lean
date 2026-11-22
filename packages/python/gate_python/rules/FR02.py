"""FR02: Pass-through: a function whose body is one call that forwards at least 80% of its parameters."""

import ast

from gate_python import effect_uses, signature
from gate_python.findings import on_function
from gate_python.model import Analysis, Finding, FunctionInfo
from gate_python.syntax import statements_of

RULE = "FR02"
ADVICE = "Call the target directly, or give this function behaviour of its own"
PERCENT = 100
CONSTRUCTORS = ("__init__", "__new__")


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    declared = [parameter.arg for parameter in signature.parameters(function)]
    forwarded = [name for name in declared if name in _forwarded(function)]
    limit = analysis.config.limits[RULE]
    exempt = function.name in CONSTRUCTORS or _is_effect_wrapper(function, analysis)
    if not declared or exempt or len(forwarded) * PERCENT < limit * len(declared):
        return []
    share = len(forwarded) * PERCENT // len(declared)
    return [on_function(RULE, function, f"function {function.name} is one call forwarding {share}% of its parameters, limit {limit}%. {ADVICE}")]


def _is_effect_wrapper(function: FunctionInfo, analysis: Analysis) -> bool:
    """True for a function in an effect component that uses an effect API: the wrapper FX01 asks for."""
    in_effect_component = analysis.program.modules[function.path].effect
    return in_effect_component and bool(effect_uses.in_function(function, analysis.program))


def _forwarded(function: FunctionInfo) -> set[str]:
    """Parameter names passed unchanged by the one call that is the whole body; empty when the body is anything else."""
    body = function.node.body if isinstance(function.node, ast.Lambda) else statements_of(function.node)
    match body:
        case ast.Call() as call:
            return _passed(call)
        case [ast.Return(value=ast.Call() as call)] | [ast.Expr(value=ast.Call() as call)]:
            return _passed(call)
        case [ast.Return(value=ast.Await(value=ast.Call() as call))] | [ast.Expr(value=ast.Await(value=ast.Call() as call))]:
            return _passed(call)
    return set()


def _passed(call: ast.Call) -> set[str]:
    values = [*call.args, *(keyword.value for keyword in call.keywords)]
    plain = [value.value if isinstance(value, ast.Starred) else value for value in values]
    return {value.id for value in plain if isinstance(value, ast.Name)}
