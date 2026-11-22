"""AR01: Flag parameter: a boolean or enum parameter must not be the condition of a branch in the function body."""

import ast

from gate_python import members, scopes, signature
from gate_python.findings import on_line
from gate_python.model import Analysis, Finding, FunctionInfo, Program, TypeRef
from gate_python.syntax import body_nodes

RULE = "AR01"
ADVICE = "Split the function into one function per branch and let the caller choose"
BOOLEAN = "builtins.bool"
EQUALITY = (ast.Eq, ast.NotEq, ast.Is, ast.IsNot)


def check(function: FunctionInfo, analysis: Analysis) -> list[Finding]:
    env = scopes.scope_of(function, analysis.program).env
    flags = [parameter for parameter in signature.parameters(function) if _is_flag(env[parameter.arg], analysis.program)]
    branched = {name for node in body_nodes(function.node) for name in _condition_names(node)}
    return [
        on_line(RULE, function.path, flag.lineno, f"parameter {flag.arg} of function {function.name} is a flag that selects a branch. {ADVICE}")
        for flag in flags
        if flag.arg in branched
    ]


def _is_flag(kind: TypeRef, program: Program) -> bool:
    return kind.name == BOOLEAN or (kind.name in program.classes and members.is_enum(kind.name, program))


def _condition_names(node: ast.AST) -> set[str]:
    """Names that are the condition of this node when it is an if, conditional expression, loop or match."""
    match node:
        case ast.If(test=condition) | ast.IfExp(test=condition) | ast.While(test=condition) | ast.Match(subject=condition):
            return _tested(condition)
    return set()


def _tested(condition: ast.expr) -> set[str]:
    match condition:
        case ast.Name(id=name):
            return {name}
        case ast.UnaryOp(op=ast.Not(), operand=operand):
            return _tested(operand)
        case ast.BoolOp(values=values):
            return {name for value in values for name in _tested(value)}
        case ast.Compare(left=left, ops=[operator], comparators=[right]) if isinstance(operator, EQUALITY):
            return _compared(left, right) | _compared(right, left)
    return set()


def _compared(variable: ast.expr, constant: ast.expr) -> set[str]:
    match (variable, constant):
        case (ast.Name(id=name), ast.Constant() | ast.Attribute()):
            return {name}
    return set()
