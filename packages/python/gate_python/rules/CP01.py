"""CP01: Law of Demeter: a member-access chain must not pass through more than one behavioural object defined in the solution. Exempt: data records, standard-library types, fluent calls returning the receiver's type."""

import ast

from gate_python import members, scopes
from gate_python.findings import on_line
from gate_python.model import Analysis, Finding, Located, Module, Scope
from gate_python.resolve import type_of, unwrapped

RULE = "CP01"
ADVICE = "Ask the first object for what you need instead of reaching through it"
WHAT = "behavioural objects"
SHOWN = 60
LINKS = (ast.Attribute, ast.Call)


def check(module: Module, analysis: Analysis) -> list[Finding]:
    chains = _chains(scopes.scoped_nodes(module, analysis.program))
    passed = [(item.node, _behavioural_steps(item)) for item in chains if isinstance(item.node, LINKS)]
    limit = analysis.config.limits[RULE]
    return [
        on_line(
            RULE,
            module.path,
            chain.lineno,
            f"chain {ast.unparse(chain)[:SHOWN]} passes through {len(objects)} {WHAT}, limit {limit}. {ADVICE}",
        )
        for chain, objects in passed
        if len(objects) > limit
    ]


def _chains(located: list[Located]) -> list[Located]:
    """Member accesses and calls that are not themselves the receiver of a longer chain."""
    links = [item for item in located if isinstance(item.node, LINKS)]
    inner = {id(_receiver(item.node)) for item in links}
    return [item for item in links if id(item.node) not in inner]


def _behavioural_steps(chain: Located) -> list[ast.AST]:
    """The intermediates of the chain that count; the last intermediate is the first receiver and is left out."""
    return [step for step in _intermediates(chain.node)[:-1] if _is_behavioural(step, chain.scope)]


def _receiver(node: ast.AST) -> ast.AST | None:
    match node:
        case ast.Attribute(value=inner) | ast.Call(func=inner):
            return inner
    return None


def _intermediates(node: ast.AST) -> list[ast.AST]:
    """The dereferenced results in the chain, outermost first; the last one is the chain's first receiver."""
    match node:
        case ast.Attribute(value=inner):
            return [inner, *_intermediates(inner)]
        case ast.Call(func=inner):
            return _intermediates(inner)
    return []


def _is_behavioural(step: ast.AST, scope: Scope) -> bool:
    """True when the step's type is under the root, has a method, and is not a fluent return of its receiver's type."""
    kind = unwrapped(type_of(step, scope)).name
    if kind not in scope.program.classes or not members.methods_of(kind, scope.program):
        return False
    match step:
        case ast.Call(func=ast.Attribute(value=receiver)):
            return unwrapped(type_of(receiver, scope)).name != kind
    return True
