"""Finds the places where code directly uses an effect API or assigns mutable global state."""

import ast
from dataclasses import dataclass

from gate_python import effect_apis, scopes, state
from gate_python.model import FunctionInfo, Located, Module, Program, Scope
from gate_python.resolve import UNANNOTATED, qualified_of, type_of, unwrapped


@dataclass(frozen=True)
class EffectUse:
    line: int
    api: str
    group: str


def in_function(function: FunctionInfo, program: Program) -> list[EffectUse]:
    scope = scopes.scope_of(function, program)
    assigned = [EffectUse(node.lineno, ast.unparse(node), effect_apis.GLOBAL_STATE) for node in state.global_writes(function, scope)]
    return [*_api_uses(scopes.function_nodes(function, program)), *assigned]


def in_module(module: Module, program: Program) -> list[EffectUse]:
    """Every effect use in the module, inside and outside functions."""
    top = scopes.top_nodes(module, program)
    inside = [use for function in scopes.functions_in(module, program) for use in in_function(function, program)]
    return [*_api_uses(top), *inside]


def _api_uses(located: list[Located]) -> list[EffectUse]:
    named = [
        (item.node.lineno, _reference(item.node, item.scope), _base_reference(item.node, item.scope))
        for item in located
        if isinstance(item.node, (ast.Name, ast.Attribute))
    ]
    return [
        EffectUse(line, name, effect_apis.group_of(name))
        for line, name, base in named
        if effect_apis.group_of(name) and not effect_apis.group_of(base)
    ]


def _base_reference(node: ast.AST, scope: Scope) -> str:
    return _reference(node.value, scope) if isinstance(node, ast.Attribute) else ""


def _reference(node: ast.AST, scope: Scope) -> str:
    """The qualified name a name or attribute stands for, through imports or through the type of its owner."""
    static = qualified_of(node, scope)
    if static or not isinstance(node, ast.Attribute):
        return static
    owner = unwrapped(type_of(node.value, scope))
    return f"{owner.name}.{node.attr}" if owner.name and owner != UNANNOTATED else ""
