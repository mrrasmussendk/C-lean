"""Walks over syntax trees that stop at the border of the function, lambda or class they start in."""

import ast
from typing import Iterator

from gate_python.model import FunctionNode

SCOPES = (ast.FunctionDef, ast.AsyncFunctionDef, ast.Lambda, ast.ClassDef)
DEFINITIONS = (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)
WRITTEN = (ast.Store, ast.Del)


def own_nodes(root: ast.AST) -> Iterator[ast.AST]:
    """Every node below root in source order, without the insides of nested functions and classes."""
    for child in ast.iter_child_nodes(root):
        yield child
        if not isinstance(child, SCOPES):
            yield from own_nodes(child)


def body_of(function: FunctionNode) -> list[ast.stmt | ast.expr]:
    if isinstance(function, ast.Lambda):
        return [function.body]
    return list(function.body)


def body_nodes(function: FunctionNode) -> Iterator[ast.AST]:
    for part in body_of(function):
        yield part
        if not isinstance(part, SCOPES):
            yield from own_nodes(part)


def has_docstring(body: list[ast.stmt]) -> bool:
    match body:
        case [ast.Expr(value=ast.Constant(value=str())), *_]:
            return True
    return False


def statements_of(function: FunctionNode) -> list[ast.stmt]:
    """The body statements without a leading documentation string; none for a lambda."""
    if isinstance(function, ast.Lambda):
        return []
    return function.body[1:] if has_docstring(function.body) else list(function.body)


def root_name(node: ast.AST) -> str:
    """The variable an attribute or element access starts from, or nothing."""
    match node:
        case ast.Attribute(value=value) | ast.Subscript(value=value):
            return root_name(value)
        case ast.Name(id=name):
            return name
    return ""


def is_member_write(node: ast.AST) -> bool:
    match node:
        case ast.Attribute(ctx=context) | ast.Subscript(ctx=context):
            return isinstance(context, WRITTEN)
    return False


def declared_outside(function: FunctionNode) -> frozenset[str]:
    """Names the function declares global or nonlocal."""
    nodes = [node for node in body_nodes(function) if isinstance(node, (ast.Global, ast.Nonlocal))]
    return frozenset(name for node in nodes for name in node.names)


def is_dunder(name: str) -> bool:
    return name.startswith("__") and name.endswith("__")


def decorator_names(node: ast.AST) -> list[str]:
    if not isinstance(node, DEFINITIONS):
        return []
    return [ast.unparse(decorator).split("(")[0].split(".")[-1] for decorator in node.decorator_list]


def parsed_expression(text: str) -> ast.expr | None:
    try:
        return ast.parse(text.strip(), mode="eval").body
    except SyntaxError:
        return None
