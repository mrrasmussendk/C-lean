"""DC03: A numeric literal other than -1, 0 and 1 must be a named constant in production code."""

import ast

from gate_python import members
from gate_python.findings import on_line
from gate_python.model import Analysis, Finding, Module, Program

RULE = "DC03"
ADVICE = "Give the value a name: an UPPER_CASE constant at module level"
ALLOWED = (0, 1)


def check(module: Module, analysis: Analysis) -> list[Finding]:
    constants = [node for node in ast.walk(module.tree) if isinstance(node, ast.Constant)]
    literals = [node for node in constants if _is_number(node.value) and node.value not in ALLOWED]
    named = {id(literal) for literal in _constant_initializers(module, analysis.program)}
    return [
        on_line(RULE, module.path, literal.lineno, f"numeric literal {literal.value!r} is not a named constant; only -1, 0 and 1 may be written inline. {ADVICE}")
        for literal in literals
        if id(literal) not in named
    ]


def _is_number(value: object) -> bool:
    return isinstance(value, (int, float, complex)) and not isinstance(value, bool)


def _constant_initializers(module: Module, program: Program) -> list[ast.AST]:
    """Literals that are the whole initializer of an UPPER_CASE name or of an enum member."""
    classes = [info for info in program.classes.values() if info.path == module.path]
    enums = [info.node for info in classes if members.is_enum(info.qualified, program)]
    plain: list[ast.Module | ast.ClassDef] = [module.tree, *(info.node for info in classes)]
    upper = [value for owner in plain for name, value in _initializers(owner.body) if name.isupper()]
    return [*upper, *(value for owner in enums for _, value in _initializers(owner.body))]


def _initializers(body: list[ast.stmt]) -> list[tuple[str, ast.AST]]:
    return [pair for statement in body for pair in _initializer(statement)]


def _initializer(statement: ast.stmt) -> list[tuple[str, ast.AST]]:
    match statement:
        case ast.Assign(targets=[ast.Name(id=name)], value=value) | ast.AnnAssign(target=ast.Name(id=name), value=ast.expr() as value):
            return [(name, value.operand if isinstance(value, ast.UnaryOp) else value)]
    return []
