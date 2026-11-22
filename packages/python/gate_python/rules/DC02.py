"""DC02: Comments must not contain code."""

import ast
import textwrap

from gate_python.findings import on_line
from gate_python.model import Analysis, Comment, Finding, Module

RULE = "DC02"
ADVICE = "Delete the commented-out code; version control keeps it"
SHOWN = 40
CODE = (
    ast.Assign, ast.AugAssign, ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef, ast.Return, ast.Import,
    ast.ImportFrom, ast.If, ast.For, ast.AsyncFor, ast.While, ast.Try, ast.With, ast.AsyncWith, ast.Match,
)


def check(module: Module, analysis: Analysis) -> list[Finding]:
    blocks = _blocks([comment for comment in module.comments if comment.alone])
    trailing = [[comment] for comment in module.comments if not comment.alone]
    coded = [comment for block in [*blocks, *trailing] for comment in _code_comments(block)]
    return [
        on_line(RULE, module.path, comment.line, f"comment '{comment.text[:SHOWN]}' holds code. {ADVICE}")
        for comment in coded
    ]


def _blocks(comments: list[Comment]) -> list[list[Comment]]:
    """Runs of comment-only lines that directly follow each other."""
    starts = [index for index, comment in enumerate(comments) if index == 0 or comments[index - 1].line != comment.line - 1]
    return [comments[start:end] for start, end in zip(starts, [*starts[1:], len(comments)])]


def _code_comments(block: list[Comment]) -> list[Comment]:
    """The first comment when the block as a whole is code, else each comment that is code by itself."""
    if _is_code("\n".join(_content(comment) for comment in block)):
        return block[:1]
    return [comment for comment in block if _is_code(_content(comment))]


def _content(comment: Comment) -> str:
    return comment.text[1:].removeprefix(" ")


def _is_code(text: str) -> bool:
    try:
        statements = ast.parse(textwrap.dedent(text)).body
    except (SyntaxError, ValueError):
        return False
    return any(_is_code_statement(statement) for statement in statements)


def _is_code_statement(statement: ast.stmt) -> bool:
    match statement:
        case ast.Expr(value=ast.Call()) | ast.Expr(value=ast.Await(value=ast.Call())):
            return True
        case ast.AnnAssign(value=value):
            return value is not None
    return isinstance(statement, CODE)
