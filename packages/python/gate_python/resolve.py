"""Resolves names, annotations, expression types and call targets from imports and annotations alone."""

import ast

from gate_python import members
from gate_python.bindings import BUILTINS, canonical, in_root, split_module
from gate_python.model import CLASS, EXTERNAL, FUNCTION, UNRESOLVED, UNTYPED, VALUE, Program, Scope, Target, TypeRef
from gate_python.syntax import parsed_expressions

UNKNOWN = TypeRef("")
UNANNOTATED = TypeRef("?")
NOTHING = "None"
UNION = "typing.Union"
OPTIONALS = (UNION, "typing.Optional")
SEQUENCES = frozenset(
    {
        "builtins.list",
        "builtins.set",
        "builtins.frozenset",
        "builtins.tuple",
        "typing.List",
        "typing.Set",
        "typing.FrozenSet",
        "typing.Tuple",
        "typing.Sequence",
        "typing.Iterable",
        "typing.Iterator",
        "typing.Collection",
        "collections.abc.Sequence",
        "collections.abc.Iterable",
        "collections.abc.Iterator",
        "collections.abc.Collection",
        "collections.abc.MutableSequence",
        "collections.deque",
    }
)
VALUE_TYPES = frozenset(
    {
        "builtins.str",
        "builtins.int",
        "builtins.float",
        "builtins.bool",
        "builtins.bytes",
        "builtins.list",
        "builtins.dict",
        "builtins.set",
        "builtins.frozenset",
        "builtins.tuple",
    }
)
LIST = "builtins.list"


def qualified_of(node: ast.AST, scope: Scope) -> str:
    """The dotted name of the module, class, function or module variable an expression names, or nothing."""
    match node:
        case ast.Name(id=name):
            return _name_qualified(name, scope)
        case ast.Attribute(value=value, attr=attr):
            base = qualified_of(value, scope)
            return canonical(f"{base}.{attr}", scope.program) if _is_namespace(base, scope.program) else ""
    return ""


def _name_qualified(name: str, scope: Scope) -> str:
    if name in scope.env:
        return scope.env[name].target
    table = scope.program.bindings[scope.module.path]
    if name in table:
        return canonical(table[name], scope.program)
    return f"builtins.{name}" if name in BUILTINS else ""


def _is_namespace(base: str, program: Program) -> bool:
    if not base or not in_root(base, program):
        return bool(base)
    return base in program.classes or base in program.namespaces


def annotation_type(annotation: ast.expr | None, scope: Scope) -> TypeRef:
    match annotation:
        case ast.Constant(value=str() as text):
            return annotation_type(next(iter(parsed_expressions(text)), None), scope)
        case ast.Constant(value=None):
            return TypeRef(NOTHING)
        case ast.Name() | ast.Attribute():
            return TypeRef(qualified_of(annotation, scope))
        case ast.Subscript(value=head, slice=inner):
            return TypeRef(qualified_of(head, scope), tuple(annotation_type(part, scope) for part in _parts(inner)))
        case ast.BinOp(left=left, op=ast.BitOr(), right=right):
            return TypeRef(UNION, (annotation_type(left, scope), annotation_type(right, scope)))
    return UNKNOWN


def _parts(inner: ast.expr) -> list[ast.expr]:
    return list(inner.elts) if isinstance(inner, ast.Tuple) else [inner]


def unwrapped(kind: TypeRef) -> TypeRef:
    """The type itself, or the one alternative of an optional type that is not None."""
    present = [part for part in kind.args if part.name != NOTHING]
    if kind.name in OPTIONALS and len(present) == 1:
        return unwrapped(present[0])
    return kind


def type_of(node: ast.AST, scope: Scope) -> TypeRef:
    match node:
        case ast.Name(id=name):
            return _variable_type(name, scope)
        case ast.Constant(value=value):
            return TypeRef(f"builtins.{type(value).__name__}")
        case ast.Call():
            return _call_type(node, scope)
        case ast.Attribute():
            return _attribute_type(node, scope)
        case ast.Subscript(value=value):
            return _item_type(type_of(value, scope))
        case ast.Await(value=value):
            return type_of(value, scope)
        case ast.ListComp(elt=element) | ast.List(elts=[element, *_]):
            return TypeRef(LIST, (type_of(element, scope),))
    return _literal_type(node)


def _literal_type(node: ast.AST) -> TypeRef:
    match node:
        case ast.JoinedStr():
            return TypeRef("builtins.str")
        case ast.List():
            return TypeRef(LIST)
        case ast.Dict() | ast.DictComp():
            return TypeRef("builtins.dict")
        case ast.Set() | ast.SetComp():
            return TypeRef("builtins.set")
        case ast.Tuple():
            return TypeRef("builtins.tuple")
    return UNKNOWN


def _variable_type(name: str, scope: Scope) -> TypeRef:
    if name in scope.env:
        return scope.env[name]
    return scope.program.envs.get(scope.module.path, {}).get(name, UNKNOWN)


def _item_type(container: TypeRef) -> TypeRef:
    if not container.args:
        return UNKNOWN
    return container.args[0] if container.name in SEQUENCES else container.args[-1]


def element_type(container: TypeRef) -> TypeRef:
    """The type a loop over the container yields."""
    return container.args[0] if container.args else UNKNOWN


def _call_type(node: ast.Call, scope: Scope) -> TypeRef:
    target = target_of(node.func, scope)
    if target.kind == CLASS:
        return TypeRef(target.name)
    if target.kind == FUNCTION:
        return return_type(target.name, scope.program)
    if target.kind == EXTERNAL and (target.name in VALUE_TYPES or target.name.split(".")[-1][:1].isupper()):
        return TypeRef(target.name)
    return UNKNOWN


def return_type(qualified: str, program: Program) -> TypeRef:
    function = program.functions[qualified]
    if isinstance(function.node, ast.Lambda):
        return UNKNOWN
    return annotation_type(function.node.returns, Scope(program, program.modules[function.path], {}))


def _attribute_type(node: ast.Attribute, scope: Scope) -> TypeRef:
    static = qualified_of(node.value, scope)
    if static in scope.program.classes and members.is_enum(static, scope.program):
        return TypeRef(static)
    owner = static if static in scope.program.classes else unwrapped(type_of(node.value, scope)).name
    return members.field_type(owner, node.attr, scope.program)


def target_of(node: ast.AST, scope: Scope) -> Target:
    """What a called expression leads to: a function or class under the root, something external, or nothing."""
    static = qualified_of(node, scope)
    if static:
        return _static_target(static, scope.program)
    match node:
        case ast.Name(id=name):
            return _local_target(name, scope)
        case ast.Attribute():
            return _member_target(node, scope)
    return Target(VALUE, "")


def _static_target(qualified: str, program: Program) -> Target:
    if qualified in program.functions:
        return Target(FUNCTION, qualified)
    if qualified in program.classes:
        return Target(CLASS, qualified)
    if not in_root(qualified, program):
        return Target(EXTERNAL, qualified)
    owner, _, member = qualified.rpartition(".")
    if owner in program.classes:
        return members.lookup(owner, member, program)
    return Target(VALUE if _is_bound(qualified, program) else UNRESOLVED, qualified)


def _is_bound(qualified: str, program: Program) -> bool:
    module, rest = split_module(qualified, program)
    if not module or not rest:
        return True
    return rest[0] in program.bindings[program.paths[module]]


def _local_target(name: str, scope: Scope) -> Target:
    known = name in scope.env or name in scope.program.envs.get(scope.module.path, {})
    if not known:
        return Target(UNRESOLVED, name)
    return Target(UNTYPED if _variable_type(name, scope) == UNANNOTATED else VALUE, name)


def _member_target(node: ast.Attribute, scope: Scope) -> Target:
    owner = unwrapped(type_of(node.value, scope))
    if owner == UNANNOTATED:
        return Target(UNTYPED, ast.unparse(node))
    if owner.name in scope.program.classes:
        return members.lookup(owner.name, node.attr, scope.program)
    if not owner.name or in_root(owner.name, scope.program):
        return Target(VALUE, "")
    return Target(EXTERNAL, f"{owner.name}.{node.attr}")
