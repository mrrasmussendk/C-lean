"""The declared parameters of a function, with and without its receiver."""

import ast

from gate_python.model import FunctionInfo
from gate_python.syntax import decorator_names

STATIC = "staticmethod"
CLASS_METHOD = "classmethod"


def positional(function: FunctionInfo) -> list[ast.arg]:
    """Positional parameters without the receiver."""
    declared = [*function.node.args.posonlyargs, *function.node.args.args]
    return declared[1:] if receiver(function) else declared


def receiver(function: FunctionInfo) -> str:
    """The name of self or cls when the function is a method that has one."""
    declared = [*function.node.args.posonlyargs, *function.node.args.args]
    if not function.owner or not declared or STATIC in decorator_names(function.node):
        return ""
    return declared[0].arg


def variadic(function: FunctionInfo) -> list[ast.arg]:
    arguments = function.node.args
    return [extra for extra in (arguments.vararg, arguments.kwarg) if extra is not None]


def parameters(function: FunctionInfo) -> list[ast.arg]:
    """Every declared parameter without the receiver; a variadic parameter is one parameter."""
    return [*positional(function), *function.node.args.kwonlyargs, *variadic(function)]


def defaults(function: FunctionInfo) -> dict[str, ast.expr]:
    arguments = function.node.args
    declared = [*arguments.posonlyargs, *arguments.args]
    trailing = declared[len(declared) - len(arguments.defaults):]
    pairs = [*zip(trailing, arguments.defaults), *zip(arguments.kwonlyargs, arguments.kw_defaults)]
    return {parameter.arg: value for parameter, value in pairs if value is not None}


def is_class_method(function: FunctionInfo) -> bool:
    return CLASS_METHOD in decorator_names(function.node)
