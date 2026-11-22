"""Lists the mutants of production code: one per replacement of an operator or literal token."""

import ast
import tokenize
from dataclasses import dataclass, replace
from typing import Any, Mapping

from gate_python import lexing, members, scopes
from gate_python.configuration import LineRange
from gate_python.logcalls import is_log_call
from gate_python.model import Located, Module, Program
from gate_python.resolve import type_of
from gate_python.rule_table import SPEC_VERSION
from gate_python.runtime_names import HELPER_FILES

LANGUAGE = "python"
ARITHMETIC = "ArithmeticOperator"
EQUALITY = "EqualityOperator"
LOGICAL = "LogicalOperator"
BOOLEAN = "BooleanLiteral"
UNARY = "UnaryOperator"
SWAPS: Mapping[str, Mapping[str, str]] = {
    ARITHMETIC: {"+": "-", "-": "+", "*": "/", "/": "*"},
    EQUALITY: {"==": "!=", "!=": "==", "<": "<=", "<=": "<", ">": ">=", ">=": ">"},
    LOGICAL: {"and": "or", "or": "and"},
    BOOLEAN: {"True": "False", "False": "True"},
    UNARY: {"not": ""},
}
ARITHMETIC_NODES = (ast.Add, ast.Sub, ast.Mult, ast.Div)
STRING = "builtins.str"
LOGGING = "logging"
GUARD = "guard"
COMPILE_ERROR = "CompileError"
MUTABLE_TOKENS = (tokenize.OP, tokenize.NAME)


@dataclass(frozen=True)
class Span:
    start: int
    end: int


@dataclass(frozen=True)
class Gap:
    """A stretch of bytes in which the token of one mutator is looked for, and the expression it belongs to."""

    mutator: str
    span: Span
    context: str
    outer: Span


@dataclass(frozen=True)
class Piece:
    """One token with its position in bytes and in lines and columns."""

    text: str
    start: int
    end: int
    line: int
    column: int


@dataclass(frozen=True)
class Mutant:
    """One replacement of one token; outer is the expression that a switch would wrap."""

    name: str
    file: str
    piece: Piece
    mutator: str
    context: str
    outer: Span


def found(program: Program, wanted: tuple[LineRange, ...] | None) -> list[Mutant]:
    """The mutants of production code on the wanted lines, sorted, each with a unique id as its name."""
    production = [module for module in program.modules.values() if not module.test and module.path not in HELPER_FILES]
    every = [mutant for module in production for mutant in _mutants(module, program)]
    kept = [mutant for mutant in every if wanted is None or any(_covers(area, mutant) for area in wanted)]
    ordered = sorted(kept, key=_order)
    names = [mutant.name for mutant in ordered]
    return [replace(mutant, name=_unique(mutant.name, names[:index])) for index, mutant in enumerate(ordered)]


def document(mutants: list[Mutant], failed: frozenset[str] | None) -> dict[str, Any]:
    """mutants.json; failed holds the ids that could not be switched in, or is None when nothing was switched."""
    backed_out = failed or frozenset()
    rows = [_row(mutant) | ({"status": COMPILE_ERROR} if mutant.name in backed_out else {}) for mutant in mutants]
    return {"spec": SPEC_VERSION, "language": LANGUAGE, "switched": failed is not None, "mutants": rows}


def _row(mutant: Mutant) -> dict[str, Any]:
    piece = mutant.piece
    return {
        "id": mutant.name,
        "file": mutant.file,
        "line": piece.line,
        "column": piece.column,
        "mutator": mutant.mutator,
        "start": piece.start,
        "end": piece.end,
        "original": piece.text,
        "replacement": replacement_of(mutant),
        "context": mutant.context,
    }


def replacement_of(mutant: Mutant) -> str:
    return SWAPS[mutant.mutator][mutant.piece.text]


def _covers(area: LineRange, mutant: Mutant) -> bool:
    return area.path == mutant.file and area.start <= mutant.piece.line <= area.end


def _order(mutant: Mutant) -> tuple[str, int, str]:
    return (mutant.file, mutant.piece.start, mutant.mutator)


def _unique(name: str, earlier: list[str]) -> str:
    """The id itself, or the id with #<n> when earlier mutants already have it."""
    same = earlier.count(name)
    return f"{name}#{same + 1}" if same else name


def _mutants(module: Module, program: Program) -> list[Mutant]:
    starts = line_starts(module)
    pieces = [_piece(token, module, starts) for token in lexing.tokens_of(module.text) if token.type in MUTABLE_TOKENS]
    located = scopes.scoped_nodes(module, program)
    skipped = _below(_unmutated_roots(module, program))
    logged = _below([part for item in located for part in _log_arguments(item.node)])
    guarded = _below([item.node.test for item in located if isinstance(item.node, ast.If) and _only_raises(item.node)])
    contexts = {LOGGING: logged, GUARD: guarded}
    gaps = [gap for item in located if id(item.node) not in skipped for gap in _gaps(item, starts, contexts)]
    return [mutant for gap in gaps for mutant in _mutant(gap, pieces, module)]


def line_starts(module: Module) -> list[int]:
    """The byte offset at which each line of the stored file starts."""
    lengths = [len(line.encode("utf-8")) + 1 for line in module.text.split("\n")]
    return [module.prefix + sum(lengths[:index]) for index in range(len(lengths))]


def _piece(token: tokenize.TokenInfo, module: Module, starts: list[int]) -> Piece:
    line = module.text.split("\n")[token.start[0] - 1]
    start = starts[token.start[0] - 1] + len(line[: token.start[1]].encode("utf-8"))
    return Piece(token.string, start, start + len(token.string.encode("utf-8")), token.start[0], token.start[1] + 1)


def _below(roots: list[ast.AST]) -> set[int]:
    return {id(node) for root in roots for node in ast.walk(root)}


def _unmutated_roots(module: Module, program: Program) -> list[ast.AST]:
    """Type annotations, UPPER_CASE module constants and enum member values."""
    annotated = [node.annotation for node in ast.walk(module.tree) if isinstance(node, ast.AnnAssign)]
    constants = [statement for statement in module.tree.body if _is_constant(statement)]
    enums = [info.node for info in program.classes.values() if info.path == module.path and members.is_enum(info.qualified, program)]
    values = [statement for owner in enums for statement in owner.body if isinstance(statement, (ast.Assign, ast.AnnAssign))]
    return [*annotated, *constants, *values]


def _is_constant(statement: ast.stmt) -> bool:
    match statement:
        case ast.Assign(targets=[ast.Name(id=name)]) | ast.AnnAssign(target=ast.Name(id=name)):
            return name.isupper()
    return False


def _log_arguments(node: ast.AST) -> list[ast.AST]:
    match node:
        case ast.Call(func=called, args=arguments, keywords=keywords) if is_log_call(called):
            return [*arguments, *keywords]
    return []


def _only_raises(node: ast.If) -> bool:
    return all(isinstance(statement, ast.Raise) for statement in node.body)


def _gaps(item: Located, starts: list[int], contexts: Mapping[str, set[int]]) -> list[Gap]:
    """Where the node's own operator or literal tokens lie."""
    node = item.node
    if not isinstance(node, ast.expr):
        return []
    context = "".join([name for name, ids in contexts.items() if id(node) in ids][:1])
    outer = Span(_start(node, starts), _end(node, starts))
    match node:
        case ast.BinOp(left=left, op=operator, right=right) if isinstance(operator, ARITHMETIC_NODES) and not _joins_strings(item):
            return [Gap(ARITHMETIC, Span(_end(left, starts), _start(right, starts)), context, outer)]
        case ast.Compare(left=left, comparators=others):
            return [Gap(EQUALITY, span, context, outer) for span in _between([left, *others], starts)]
        case ast.BoolOp(values=values):
            return [Gap(LOGICAL, span, context, outer) for span in _between(values, starts)]
        case ast.Constant(value=bool()):
            return [Gap(BOOLEAN, outer, context, outer)]
        case ast.UnaryOp(op=ast.Not(), operand=operand):
            return [Gap(UNARY, Span(outer.start, _start(operand, starts)), context, outer)]
    return []


def _joins_strings(item: Located) -> bool:
    match item.node:
        case ast.BinOp(left=left, op=ast.Add(), right=right):
            return STRING in (type_of(left, item.scope).name, type_of(right, item.scope).name)
    return False


def _between(operands: list[ast.expr], starts: list[int]) -> list[Span]:
    pairs = zip(operands, operands[1:])
    return [Span(_end(before, starts), _start(after, starts)) for before, after in pairs]


def _start(node: ast.expr, starts: list[int]) -> int:
    return starts[node.lineno - 1] + node.col_offset


def _end(node: ast.expr, starts: list[int]) -> int:
    return starts[(node.end_lineno or node.lineno) - 1] + (node.end_col_offset or 0)


def _mutant(gap: Gap, pieces: list[Piece], module: Module) -> list[Mutant]:
    """The mutant of the first token in the gap that the mutator replaces."""
    swaps = SWAPS[gap.mutator]
    inside = [piece for piece in pieces if gap.span.start <= piece.start and piece.end <= gap.span.end and piece.text in swaps]
    return [
        Mutant(f"{module.path}:{piece.line}:{piece.column}:{gap.mutator}", module.path, piece, gap.mutator, gap.context, gap.outer)
        for piece in inside[:1]
    ]
