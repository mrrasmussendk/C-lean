"""Hands out the scope of a function or module, and every node of a module together with its scope."""

import ast

from gate_python.model import FunctionInfo, Located, Module, Program, Scope
from gate_python.syntax import body_nodes, own_nodes


def scope_of(function: FunctionInfo, program: Program) -> Scope:
    return Scope(program, program.modules[function.path], program.envs[function.key])


def module_scope(module: Module, program: Program) -> Scope:
    return Scope(program, module, {})


def functions_in(module: Module, program: Program) -> list[FunctionInfo]:
    return [function for function in program.all_functions if function.path == module.path]


def scoped_nodes(module: Module, program: Program) -> list[Located]:
    """Every node of the module outside signatures: top level, class bodies and function bodies."""
    top = module_scope(module, program)
    classes = [info.node for info in program.classes.values() if info.path == module.path]
    outer = [Located(node, top) for root in [module.tree, *classes] for node in own_nodes(root)]
    return [*outer, *(item for function in functions_in(module, program) for item in function_nodes(function, program))]


def function_nodes(function: FunctionInfo, program: Program) -> list[Located]:
    scope = scope_of(function, program)
    return [Located(node, scope) for node in body_nodes(function.node)]


def calls_in(located: list[Located]) -> list[Located]:
    return [item for item in located if isinstance(item.node, ast.Call)]
