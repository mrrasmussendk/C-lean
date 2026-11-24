"""Works out the types of the local names of every function and of the variables at the top of every module."""

import ast
from dataclasses import replace
from typing import Iterable, Mapping

from gate_python import bindings, signature
from gate_python.model import FunctionInfo, Module, Program, Scope, TypeRef
from gate_python.resolve import UNANNOTATED, UNKNOWN, annotation_type, element_type, entered_type, type_of
from gate_python.syntax import body_nodes, declared_outside, own_nodes

BOOLEAN = TypeRef("builtins.bool")


def environments(program: Program) -> dict[str, Mapping[str, TypeRef]]:
    """Local types by function key, and module variable types by module path."""
    staged = replace(program, envs={module.path: _module_types(module, program) for module in program.modules.values()})
    envs: dict[str, Mapping[str, TypeRef]] = dict(staged.envs)
    for function in sorted(staged.all_functions, key=_depth):
        outer = envs[staged.functions[function.parent].key] if function.parent else {}
        envs[function.key] = _function_types(function, Scope(staged, staged.modules[function.path], outer))
    return envs


def _depth(function: FunctionInfo) -> int:
    return function.qualified.count(".")


def _module_types(module: Module, program: Program) -> dict[str, TypeRef]:
    return local_types(own_nodes(module.tree), module.name, Scope(program, module, {}), {})


def _function_types(function: FunctionInfo, scope: Scope) -> dict[str, TypeRef]:
    declared = parameter_types(function, scope)
    found = local_types(body_nodes(function.node), function.qualified, scope, declared)
    outside = declared_outside(function.node)
    return {name: kind for name, kind in found.items() if name not in outside}


def parameter_types(function: FunctionInfo, scope: Scope) -> dict[str, TypeRef]:
    given = signature.defaults(function)
    types = {extra.arg: UNKNOWN for extra in signature.variadic(function)}
    for parameter in [*signature.positional(function), *function.node.args.kwonlyargs]:
        types[parameter.arg] = _parameter_type(parameter, given.get(parameter.arg), scope)
    if signature.receiver(function):
        types[signature.receiver(function)] = TypeRef(function.owner)
    if isinstance(function.node, ast.Lambda):
        return {name: _lambda_type(kind) for name, kind in types.items()}
    return types


def _lambda_type(kind: TypeRef) -> TypeRef:
    """A lambda cannot annotate its parameters, so one without a type is unknown, not left unannotated."""
    return UNKNOWN if kind == UNANNOTATED else kind


def _parameter_type(parameter: ast.arg, default: ast.expr | None, scope: Scope) -> TypeRef:
    if parameter.annotation is not None:
        return annotation_type(parameter.annotation, scope)
    match default:
        case ast.Constant(value=bool()):
            return BOOLEAN
    return UNANNOTATED


def local_types(
    nodes: Iterable[ast.AST], prefix: str, scope: Scope, declared: Mapping[str, TypeRef]
) -> dict[str, TypeRef]:
    """A name has the type its bindings agree on, and no known type when they differ; what is declared stays.

    Declared are the parameters and, from its annotated assignment on, a variable with an annotation.
    """
    env = {**scope.env, **declared}
    fixed = set(declared)
    inferred: dict[str, TypeRef] = {}
    listed = list(nodes)
    covered = _covered_targets(listed)
    for node in [node for node in listed if id(node) not in covered]:
        for name, kind in _bindings(node, prefix, Scope(scope.program, scope.module, env)):
            if name not in fixed:
                inferred[name] = kind if isinstance(node, ast.AnnAssign) else _agreed(inferred.get(name), kind)
                env[name] = inferred[name]
        fixed.update(_annotated(node))
    return env


def _agreed(earlier: TypeRef | None, kind: TypeRef) -> TypeRef:
    """The type of a name that is bound again: the same when both bindings agree, unknown when they differ."""
    return kind if earlier is None or earlier == kind else UNKNOWN


def _annotated(node: ast.AST) -> list[str]:
    match node:
        case ast.AnnAssign(target=ast.Name(id=name)):
            return [name]
    return []


def _covered_targets(nodes: list[ast.AST]) -> set[int]:
    """The assigned names that are no binding of their own: the statement around them already says the type.

    The target of an augmented assignment is one of them: `total += 1` keeps the type `total` has.
    """
    return {id(target) for node in nodes for target in _typed_target(node)}


def _typed_target(node: ast.AST) -> list[ast.AST]:
    match node:
        case (
            ast.Assign(targets=[ast.Name() as target])
            | ast.NamedExpr(target=ast.Name() as target)
            | ast.AnnAssign(target=ast.Name() as target)
            | ast.AugAssign(target=ast.Name() as target)
        ):
            return [target]
        case (
            ast.For(target=ast.Name() as target)
            | ast.comprehension(target=ast.Name() as target)
            | ast.withitem(optional_vars=ast.Name() as target)
        ):
            return [target]
    return []


def _bindings(node: ast.AST, prefix: str, scope: Scope) -> list[tuple[str, TypeRef]]:
    match node:
        case ast.Assign(targets=[ast.Name(id=name)], value=value) | ast.NamedExpr(
            target=ast.Name(id=name), value=value
        ):
            return [(name, type_of(value, scope))]
        case ast.AnnAssign(target=ast.Name(id=name), annotation=annotation):
            return [(name, annotation_type(annotation, scope))]
        case ast.For(target=ast.Name(id=name), iter=source) | ast.comprehension(target=ast.Name(id=name), iter=source):
            return [(name, element_type(type_of(source, scope)))]
        case ast.withitem(optional_vars=ast.Name(id=name), context_expr=value):
            return [(name, entered_type(type_of(value, scope), scope.program))]
        case ast.FunctionDef(name=name) | ast.AsyncFunctionDef(name=name) | ast.ClassDef(name=name):
            return [(name, TypeRef("", (), f"{prefix}.{name}"))]
    return _plain_bindings(node, scope)


def _plain_bindings(node: ast.AST, scope: Scope) -> list[tuple[str, TypeRef]]:
    match node:
        case ast.Import() | ast.ImportFrom():
            return [(name, TypeRef("", (), origin)) for name, origin in bindings.bound_names(node, scope.module)]
        case ast.Name(id=name, ctx=ast.Store()):
            return [(name, UNKNOWN)]
        case (
            ast.ExceptHandler(name=str() as name) | ast.MatchAs(name=str() as name) | ast.MatchStar(name=str() as name)
        ):
            return [(name, UNKNOWN)]
    return []
