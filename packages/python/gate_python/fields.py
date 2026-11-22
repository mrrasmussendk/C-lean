"""Works out the types of the fields of every class under the root from annotations and constructor assignments."""

import ast

from gate_python import signature
from gate_python.environment import parameter_types
from gate_python.model import ClassInfo, FunctionInfo, Program, Scope, TypeRef
from gate_python.resolve import annotation_type, type_of
from gate_python.syntax import body_nodes, decorator_names

PROPERTY = "property"


def field_types(program: Program) -> dict[str, dict[str, TypeRef]]:
    return {info.qualified: _class_fields(info, program) for info in program.classes.values()}


def _class_fields(info: ClassInfo, program: Program) -> dict[str, TypeRef]:
    scope = Scope(program, program.modules[info.path], {})
    declared = [pair for statement in info.node.body for pair in _declared(statement, scope)]
    methods = [function for function in program.all_functions if function.owner == info.qualified]
    assigned = [pair for method in methods for pair in _assigned(method, program)]
    return dict(reversed([*declared, *assigned]))


def _declared(statement: ast.stmt, scope: Scope) -> list[tuple[str, TypeRef]]:
    match statement:
        case ast.AnnAssign(target=ast.Name(id=name), annotation=annotation):
            return [(name, annotation_type(annotation, scope))]
        case ast.Assign(targets=[ast.Name(id=name)], value=value):
            return [(name, type_of(value, scope))]
        case ast.FunctionDef(name=name, returns=returns) if PROPERTY in decorator_names(statement):
            return [(name, annotation_type(returns, scope))]
    return []


def _assigned(method: FunctionInfo, program: Program) -> list[tuple[str, TypeRef]]:
    module = program.modules[method.path]
    scope = Scope(program, module, parameter_types(method, Scope(program, module, {})))
    name = signature.receiver(method)
    return [pair for node in body_nodes(method.node) for pair in _self_binding(node, name, scope)]


def _self_binding(node: ast.AST, receiver: str, scope: Scope) -> list[tuple[str, TypeRef]]:
    match node:
        case ast.AnnAssign(target=ast.Attribute(value=ast.Name(id=owner), attr=attr), annotation=annotation):
            return [(attr, annotation_type(annotation, scope))] if owner == receiver else []
        case ast.Assign(targets=[ast.Attribute(value=ast.Name(id=owner), attr=attr)], value=value):
            return [(attr, type_of(value, scope))] if owner == receiver else []
    return []
