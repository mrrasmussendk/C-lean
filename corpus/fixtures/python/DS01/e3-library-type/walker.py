import ast


def is_call(node: ast.AST) -> bool:
    if isinstance(node, ast.Call):
        return True
    return False


def arity(node: ast.AST) -> int:
    return len(node.args) if isinstance(node, ast.Call) else 0


def label(node: ast.AST) -> str:
    match node:
        case ast.Call():
            return "call"
        case _:
            return "other"
