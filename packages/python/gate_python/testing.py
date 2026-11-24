"""What a test is, what an assertion is, and which values come from test doubles."""

import ast

from gate_python import signature
from gate_python.model import EXTERNAL, FUNCTION, FunctionInfo, Scope
from gate_python.resolve import target_of
from gate_python.syntax import body_nodes, root_name

TEST_PREFIX = "test"
ASSERT_PREFIX = "assert"
EXPECTATIONS = ("raises", "warns", "deprecated_call")
MOCK_MODULES = ("unittest.mock.", "mock.")


def is_test(function: FunctionInfo) -> bool:
    return function.name.startswith(TEST_PREFIX) and not function.parent


def assertions(function: FunctionInfo) -> list[ast.AST]:
    """Assert statements, assert* method calls and exception expectations directly in the function."""
    return [node for node in body_nodes(function.node) if _is_assertion(node)]


def _is_assertion(node: ast.AST) -> bool:
    match node:
        case ast.Assert():
            return True
        case ast.Call(func=ast.Attribute(attr=name)) | ast.Call(func=ast.Name(id=name)):
            return name.startswith(ASSERT_PREFIX) or name in EXPECTATIONS
    return False


def helper_assertions(function: FunctionInfo, scope: Scope) -> list[ast.AST]:
    """Assertions in functions of the same file that the test calls directly."""
    calls = [node for node in body_nodes(function.node) if isinstance(node, ast.Call)]
    targets = [target_of(call.func, scope) for call in calls]
    helpers = [scope.program.functions[target.name] for target in targets if target.kind == FUNCTION]
    return [found for helper in helpers if helper.path == function.path for found in assertions(helper)]


def doubles(function: FunctionInfo, scope: Scope) -> set[str]:
    """Names bound to a unittest.mock double in the test."""
    bound = {name for node in body_nodes(function.node) for name in _double(node, scope)}
    return bound | set(_patched_parameters(function, scope))


def _patched_parameters(function: FunctionInfo, scope: Scope) -> list[str]:
    """The parameters a patch decorator fills with a double: one per decorator, from the first parameter on."""
    if isinstance(function.node, ast.Lambda):
        return []
    made = [decorator.func for decorator in function.node.decorator_list if isinstance(decorator, ast.Call)]
    patches = [called for called in made if _is_mock_factory(called, scope)]
    return [parameter.arg for parameter in signature.positional(function)[: len(patches)]]


def _double(node: ast.AST, scope: Scope) -> list[str]:
    match node:
        case ast.Assign(targets=[ast.Name(id=name)], value=ast.Call(func=made)) | ast.withitem(
            optional_vars=ast.Name(id=name), context_expr=ast.Call(func=made)
        ):
            return [name] if _is_mock_factory(made, scope) else []
    return []


def _is_mock_factory(made: ast.expr, scope: Scope) -> bool:
    target = target_of(made, scope)
    return target.kind == EXTERNAL and target.name.startswith(MOCK_MODULES)


def is_call_on(node: ast.AST, names: set[str]) -> bool:
    """True for a call whose receiver chain starts at one of the names and that is not itself an assertion."""
    match node:
        case ast.Call(func=called):
            return root_name(_callee_root(called)) in names and not _is_assertion(node)
    return False


def _callee_root(called: ast.expr) -> ast.expr:
    match called:
        case ast.Attribute(value=ast.Call(func=inner)) | ast.Call(func=inner):
            return _callee_root(inner)
        case ast.Attribute(value=value):
            return _callee_root(value)
    return called


def results_of(function: FunctionInfo, names: set[str]) -> set[str]:
    """Variables assigned from a call on one of the doubles."""
    assigned = [
        node for node in body_nodes(function.node) if isinstance(node, ast.Assign) and is_call_on(node.value, names)
    ]
    return {target.id for node in assigned for target in node.targets if isinstance(target, ast.Name)}
