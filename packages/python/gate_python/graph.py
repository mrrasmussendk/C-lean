"""Extracts the dependency graph: a node per type and function, and the edges between them."""

import ast
from typing import Any

from gate_python import effect_uses, scopes
from gate_python.bindings import canonical
from gate_python.model import CLASS, FUNCTION, Analysis, ClassInfo, Edge, FunctionInfo, Program, Scope
from gate_python.privates import is_private
from gate_python.resolve import qualified_of, target_of
from gate_python.rule_table import SPEC_VERSION
from gate_python.syntax import body_nodes, decorator_names, parsed_expression

LANGUAGE = "python"
ABSTRACT_BASES = ("typing.Protocol", "typing_extensions.Protocol", "abc.ABC")
ABSTRACT_MARKERS = ("abstractmethod", "ABCMeta")
CALL = "call"
CONSTRUCT = "construct"
INHERIT = "inherit"
TYPE_REFERENCE = "type_reference"


def edges_of(program: Program) -> frozenset[Edge]:
    """Edges between nodes under the root, each once and none from a node to itself."""
    from_functions = [edge for function in program.all_functions for edge in _function_edges(function, program)]
    from_types = [edge for info in program.classes.values() for edge in _type_edges(info, program)]
    return frozenset(edge for edge in [*from_functions, *from_types] if edge.source != edge.target)


def _function_edges(function: FunctionInfo, program: Program) -> list[Edge]:
    scope = scopes.scope_of(function, program)
    calls = [node for node in body_nodes(function.node) if isinstance(node, ast.Call)]
    called = [edge for call in calls for edge in _call_edge(function, call, scope)]
    constructors = {id(call.func) for call in calls}
    named = [node for node in body_nodes(function.node) if id(node) not in constructors]
    declared = [node for node in ast.walk(function.node.args)] if isinstance(function.node, ast.Lambda) else _signature(function.node)
    types = [name for node in [*named, *declared] for name in _types_in(node, scope)]
    return [*called, *(Edge(function.symbol, program.classes[name].symbol, TYPE_REFERENCE) for name in types)]


def _signature(node: ast.FunctionDef | ast.AsyncFunctionDef) -> list[ast.AST]:
    """The annotations of the parameters and of the return value."""
    arguments = node.args
    declared = [*arguments.posonlyargs, *arguments.args, *arguments.kwonlyargs, arguments.vararg, arguments.kwarg]
    annotations = [parameter.annotation for parameter in declared if parameter is not None]
    return [part for annotation in [*annotations, node.returns] if annotation is not None for part in ast.walk(annotation)]


def _call_edge(function: FunctionInfo, call: ast.Call, scope: Scope) -> list[Edge]:
    target = target_of(call.func, scope)
    if target.kind == FUNCTION:
        return [Edge(function.symbol, scope.program.functions[target.name].symbol, CALL)]
    if target.kind == CLASS:
        return [Edge(function.symbol, scope.program.classes[target.name].symbol, CONSTRUCT)]
    return []


def _types_in(node: ast.AST | None, scope: Scope) -> list[str]:
    """The classes under the root that a name, attribute or string annotation refers to."""
    match node:
        case ast.Constant(value=str() as text):
            parsed = parsed_expression(text) if text.isidentifier() or "[" in text or "." in text else None
            return [name for part in ast.walk(parsed) for name in _types_in(part, scope)] if parsed else []
        case ast.Name(ctx=ast.Load()) | ast.Attribute(ctx=ast.Load()):
            name = qualified_of(node, scope)
            return [name] if name in scope.program.classes else []
    return []


def _type_edges(info: ClassInfo, program: Program) -> list[Edge]:
    scope = Scope(program, program.modules[info.path], {})
    bases = [canonical(base, program) for base in info.bases]
    fields = [statement.annotation for statement in info.node.body if isinstance(statement, ast.AnnAssign)]
    referenced = [name for annotation in fields for part in ast.walk(annotation) for name in _types_in(part, scope)]
    return [
        *(Edge(info.symbol, program.classes[base].symbol, INHERIT) for base in bases if base in program.classes),
        *(Edge(info.symbol, program.classes[name].symbol, TYPE_REFERENCE) for name in referenced),
    ]


def document(analysis: Analysis, edges: frozenset[Edge]) -> dict[str, Any]:
    program = analysis.program
    nodes = [*(_type_node(info, program) for info in program.classes.values()), *(_function_node(function, program) for function in program.all_functions)]
    unique = {node["id"]: node for node in reversed(nodes)}
    ordered = sorted(edges, key=_edge_order)
    return {
        "spec": SPEC_VERSION,
        "language": LANGUAGE,
        "nodes": [unique[symbol] for symbol in sorted(unique)],
        "edges": [{"from": edge.source, "to": edge.target, "kind": edge.kind} for edge in ordered],
    }


def _edge_order(edge: Edge) -> tuple[str, str, str]:
    return (edge.source, edge.target, edge.kind)


def _type_node(info: ClassInfo, program: Program) -> dict[str, Any]:
    module = program.modules[info.path]
    return {
        "id": info.symbol,
        "kind": "abstraction" if _is_abstraction(info, program) else "type",
        "component": module.component,
        "file": info.path,
        "line": info.node.lineno,
        "test": module.test,
        "public": _is_public(info.qualified, module.name),
    }


def _is_abstraction(info: ClassInfo, program: Program) -> bool:
    """A protocol, an ABC, or a class with an abstract method."""
    methods = [function for function in program.all_functions if function.owner == info.qualified]
    marked = [name for method in methods for name in decorator_names(method.node)]
    keywords = [ast.unparse(keyword.value).split(".")[-1] for keyword in info.node.keywords]
    return any(base in ABSTRACT_BASES for base in info.bases) or any(name in ABSTRACT_MARKERS for name in [*marked, *keywords])


def _function_node(function: FunctionInfo, program: Program) -> dict[str, Any]:
    module = program.modules[function.path]
    return {
        "id": function.symbol,
        "kind": "function",
        "component": module.component,
        "file": function.path,
        "line": function.node.lineno,
        "test": module.test,
        "public": not function.parent and _is_public(function.qualified, module.name),
        "effect": bool(effect_uses.in_function(function, program)),
    }


def _is_public(qualified: str, module_name: str) -> bool:
    names = qualified.removeprefix(f"{module_name}.").split(".")
    return not any(is_private(name) or name.startswith("<") for name in names)
