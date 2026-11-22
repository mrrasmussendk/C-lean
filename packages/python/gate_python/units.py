"""Builds units.json: one unit per function with its size, normalized hash, callers and statement sequences."""

import ast
from typing import Any, Sequence

from gate_python import sloc
from gate_python.graph import CALL
from gate_python.hashing import normalized_hash
from gate_python.model import Analysis, Edge, FunctionInfo
from gate_python.rule_table import SPEC_VERSION
from gate_python.syntax import SCOPES, body_nodes, body_of, statements_of

LANGUAGE = "python"
PRODUCTION_WINDOW = "DU01"
TEST_WINDOW = "TS03"


def document(analysis: Analysis, edges: frozenset[Edge]) -> dict[str, Any]:
    program = analysis.program
    production = {function.symbol for function in program.all_functions if not program.modules[function.path].test}
    calls = [edge for edge in edges if edge.kind == CALL and edge.source in production]
    units = {function.symbol: _unit(function, analysis, calls) for function in reversed(program.all_functions)}
    return {"spec": SPEC_VERSION, "language": LANGUAGE, "units": [units[symbol] for symbol in sorted(units)]}


def _unit(function: FunctionInfo, analysis: Analysis, calls: list[Edge]) -> dict[str, Any]:
    test = analysis.program.modules[function.path].test
    window = analysis.config.limits[TEST_WINDOW if test else PRODUCTION_WINDOW]
    return {
        "id": function.symbol,
        "file": function.path,
        "startLine": function.node.lineno,
        "endLine": function.node.end_lineno or function.node.lineno,
        "sloc": sloc.body_sloc(function, analysis.program),
        "hash": normalized_hash(_hashed_body(function)),
        "test": test,
        "callers": len({edge.source for edge in calls if edge.target == function.symbol}),
        "sequences": [_sequence(run) for block in _blocks(function) for run in _windows(block, window)],
    }


def _hashed_body(function: FunctionInfo) -> Sequence[ast.AST]:
    """The body without a leading documentation string; for a lambda, its expression."""
    if isinstance(function.node, ast.Lambda):
        return body_of(function.node)
    return list(statements_of(function.node))


def _blocks(function: FunctionInfo) -> list[list[ast.stmt]]:
    """The statement lists of the body: the body itself and every nested block, not those of nested functions."""
    holders = [node for node in body_nodes(function.node) if not isinstance(node, SCOPES)]
    nested = [value for holder in holders for _, value in ast.iter_fields(holder) if _is_block(value)]
    return [statements_of(function.node), *nested]


def _is_block(value: object) -> bool:
    return isinstance(value, list) and bool(value) and isinstance(value[0], ast.stmt)


def _windows(block: list[ast.stmt], size: int) -> list[list[ast.stmt]]:
    return [block[start : start + size] for start in range(len(block) - size + 1)]


def _sequence(run: list[ast.stmt]) -> dict[str, Any]:
    return {"hash": normalized_hash(run), "startLine": run[0].lineno, "endLine": run[-1].end_lineno or run[-1].lineno}
