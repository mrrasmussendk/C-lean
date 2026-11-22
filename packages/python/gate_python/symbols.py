"""Finds every function, lambda and class of a module and gives each its symbol ID."""

import ast
from dataclasses import dataclass, replace
from typing import Mapping

from gate_python import signature
from gate_python.bindings import static_name
from gate_python.model import ClassInfo, FunctionInfo, FunctionNode, Module
from gate_python.syntax import SCOPES, own_nodes

LANGUAGE = "python"
UNTYPED = "?"


@dataclass(frozen=True)
class Place:
    """Where declarations are being collected: the module, the dotted name so far, and what encloses them."""

    module: Module
    table: Mapping[str, str]
    prefix: str
    parent: str


def declarations(root: ast.AST, place: Place) -> list[FunctionInfo | ClassInfo]:
    nested = [node for node in own_nodes(root) if isinstance(node, SCOPES)]
    lambdas = [node for node in nested if isinstance(node, ast.Lambda)]
    owner = place.prefix if isinstance(root, ast.ClassDef) else ""
    found: list[FunctionInfo | ClassInfo] = []
    for node in nested:
        found += _declared(node, place, _name(node, lambdas), owner)
    return found


def _name(node: ast.AST, lambdas: list[ast.Lambda]) -> str:
    match node:
        case ast.FunctionDef(name=name) | ast.AsyncFunctionDef(name=name) | ast.ClassDef(name=name):
            return name
        case ast.Lambda():
            return f"<lambda>{lambdas.index(node) + 1}"
    return ""


def _declared(node: ast.AST, place: Place, name: str, owner: str) -> list[FunctionInfo | ClassInfo]:
    qualified = f"{place.prefix}.{name}"
    match node:
        case ast.ClassDef():
            inner = Place(place.module, place.table, qualified, place.parent)
            return [_class(node, place, qualified), *declarations(node, inner)]
        case ast.FunctionDef() | ast.AsyncFunctionDef() | ast.Lambda():
            method_of = "" if isinstance(node, ast.Lambda) else owner
            info = _function(node, place, FunctionInfo("", qualified, name, place.module.path, node, method_of, place.parent, ""))
            return [info, *declarations(node, Place(place.module, place.table, qualified, qualified))]
    return []


def _class(node: ast.ClassDef, place: Place, qualified: str) -> ClassInfo:
    bases = tuple(static_name(base, place.table) for base in node.bases)
    return ClassInfo(f"{LANGUAGE}:{_package(place.module)}:{qualified}", qualified, place.module.path, node, bases)


def _function(node: FunctionNode, place: Place, draft: FunctionInfo) -> FunctionInfo:
    types = ",".join(_type_text(parameter) for parameter in signature.parameters(draft))
    symbol = f"{LANGUAGE}:{_package(place.module)}:{draft.qualified}({types})"
    return replace(draft, symbol=symbol, key=f"{place.module.path}:{node.lineno}:{node.col_offset}")


def _type_text(parameter: ast.arg) -> str:
    match parameter.annotation:
        case None:
            return UNTYPED
        case ast.Constant(value=str() as text):
            return text.replace(" ", "")
    return ast.unparse(parameter.annotation).replace(" ", "")


def _package(module: Module) -> str:
    return module.name.split(".")[0]
