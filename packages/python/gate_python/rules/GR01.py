"""GR01: A call whose target cannot be resolved is a blocking finding, not a missing edge."""

import ast

from gate_python import scopes
from gate_python.findings import on_line
from gate_python.model import UNRESOLVED, UNTYPED, Analysis, Finding, Module
from gate_python.resolve import target_of

RULE = "GR01"
REASONS = {
    UNRESOLVED: "names nothing that is defined or imported. Define or import the target, or fix its name",
    UNTYPED: "is made on a value with no type annotation. Annotate the parameter so the target can be resolved",
}


def check(module: Module, analysis: Analysis) -> list[Finding]:
    located = scopes.scoped_nodes(module, analysis.program)
    targets = [
        (item.node, target_of(item.node.func, item.scope)) for item in located if isinstance(item.node, ast.Call)
    ]
    return [
        on_line(RULE, module.path, call.lineno, f"call to {ast.unparse(call.func)} {REASONS[target.kind]}")
        for call, target in targets
        if target.kind in REASONS
    ]
