"""What the names at the top of a module stand for, and where a dotted name leads inside the root."""

import ast
import builtins
from typing import Mapping

from gate_python.model import Module, Program
from gate_python.syntax import own_nodes

BUILTINS = frozenset(dir(builtins))
MAX_HOPS = 8
PACKAGE_PATH = "__init__.py"


def module_bindings(module: Module) -> dict[str, str]:
    return dict(pair for node in own_nodes(module.tree) for pair in bound_names(node, module))


def bound_names(node: ast.AST, module: Module) -> list[tuple[str, str]]:
    match node:
        case ast.Import(names=names):
            return [_plain_import(alias) for alias in names]
        case ast.ImportFrom(names=names):
            return [(alias.asname or alias.name, f"{_origin(node, module)}.{alias.name}") for alias in names]
        case ast.FunctionDef(name=name) | ast.AsyncFunctionDef(name=name) | ast.ClassDef(name=name):
            return [(name, f"{module.name}.{name}")]
        case ast.Name(id=name, ctx=ast.Store()):
            return [(name, f"{module.name}.{name}")]
    return []


def _plain_import(alias: ast.alias) -> tuple[str, str]:
    if alias.asname:
        return (alias.asname, alias.name)
    top = alias.name.split(".")[0]
    return (top, top)


def _origin(node: ast.ImportFrom, module: Module) -> str:
    if node.level == 0:
        return node.module or ""
    package = module.name.split(".")
    if not module.path.endswith(PACKAGE_PATH):
        package = package[:-1]
    base = package[: len(package) - (node.level - 1)]
    return ".".join([*base, *([node.module] if node.module else [])])


def static_name(node: ast.expr, table: Mapping[str, str]) -> str:
    """The dotted name an expression in a class header stands for, from the module's bindings alone."""
    match node:
        case ast.Name(id=name):
            return table.get(name, f"builtins.{name}" if name in BUILTINS else "")
        case ast.Attribute(value=value, attr=attr):
            base = static_name(value, table)
            return f"{base}.{attr}" if base else ""
        case ast.Subscript(value=value):
            return static_name(value, table)
    return ""


def in_root(qualified: str, program: Program) -> bool:
    return qualified.split(".")[0] in program.namespaces


def canonical(qualified: str, program: Program) -> str:
    """Follows re-exports until the name is the one its definition has."""
    current = qualified
    for _ in range(MAX_HOPS):
        following = _hop(current, program)
        if following == current:
            return current
        current = following
    return current


def _hop(qualified: str, program: Program) -> str:
    if qualified in program.functions or qualified in program.classes:
        return qualified
    module, rest = split_module(qualified, program)
    if not module or not rest:
        return qualified
    bound = program.bindings[program.paths[module]].get(rest[0], "")
    if not bound or bound == f"{module}.{rest[0]}":
        return qualified
    return ".".join([bound, *rest[1:]])


def split_module(qualified: str, program: Program) -> tuple[str, list[str]]:
    """The longest prefix that is a module under the root, and the names after it."""
    parts = qualified.split(".")
    for size in range(len(parts), 0, -1):
        name = ".".join(parts[:size])
        if name in program.paths:
            return (name, parts[size:])
    return ("", parts)
