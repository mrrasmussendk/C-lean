"""Rewrites a module so that each of its mutants is compiled in behind a switch."""

import ast
import json
from dataclasses import dataclass
from typing import Mapping

from gate_python import mutation
from gate_python.model import Module, Program
from gate_python.mutation import Mutant, Span
from gate_python.runtime_names import MUTANT_IDS_PLACEHOLDER, MUTANT_MODULE

FUTURE = "__future__"
ENCODING = "utf-8"


@dataclass(frozen=True)
class Rewrite:
    """The switched text of one file and the ids of its mutants that could not be switched in."""

    path: str
    text: str
    failed: tuple[str, ...]


@dataclass(frozen=True)
class Patch:
    """Bytes that take the place of a stretch of the original file."""

    span: Span
    text: bytes


@dataclass(frozen=True)
class Plan:
    """What the rewriting of one file works from: its bytes, its mutants and the index of every mutant."""

    module: Module
    data: bytes
    mutants: tuple[Mutant, ...]
    indexes: Mapping[str, int]
    preamble: Patch


def helper_text(template: str, ids: list[str]) -> str:
    """The runtime helper with the mutant ids compiled in; a switch names its mutant by its index in this list."""
    return template.replace(json.dumps([MUTANT_IDS_PLACEHOLDER]), json.dumps(ids))


def switched_files(program: Program, mutants: list[Mutant], root: str) -> list[Rewrite]:
    indexes = {mutant.name: index for index, mutant in enumerate(mutants)}
    paths = sorted({mutant.file for mutant in mutants})
    plans = [
        _plan(program.modules[path], [mutant for mutant in mutants if mutant.file == path], indexes, root)
        for path in paths
    ]
    return [_rewrite(plan) for plan in plans]


def _plan(module: Module, mutants: list[Mutant], indexes: Mapping[str, int], root: str) -> Plan:
    lines = [
        "import sys as _gate_sys",
        f"if {root!r} not in _gate_sys.path:",
        f"    _gate_sys.path.append({root!r})",
        f"import {MUTANT_MODULE}",
        "",
    ]
    at = _preamble_offset(module)
    return Plan(
        module,
        module.text.encode(module.encoding),
        tuple(mutants),
        indexes,
        Patch(Span(at, at), "\n".join(lines).encode(ENCODING)),
    )


def _preamble_offset(module: Module) -> int:
    """The byte offset of the line of the first statement that is not the module docstring or a __future__ import."""
    body = module.tree.body
    rest = [statement for statement in body if not _stays_first(statement, body)]
    starts = mutation.line_starts(module)
    if not rest:
        return module.prefix + len(module.text.encode(module.encoding))
    return starts[_first_line(rest[0]) - 1]


def _first_line(statement: ast.stmt) -> int:
    """The line the statement starts on, decorators included."""
    match statement:
        case (
            ast.FunctionDef(decorator_list=decorators)
            | ast.AsyncFunctionDef(decorator_list=decorators)
            | ast.ClassDef(decorator_list=decorators)
        ):
            return min([statement.lineno, *(decorator.lineno for decorator in decorators)])
    return statement.lineno


def _stays_first(statement: ast.stmt, body: list[ast.stmt]) -> bool:
    match statement:
        case ast.ImportFrom(module=origin):
            return origin == FUTURE
        case ast.Expr(value=ast.Constant(value=str())):
            return statement is body[0]
    return False


def _rewrite(plan: Plan) -> Rewrite:
    """All switches when the file compiles with them; else those that compile alone, the others backed out."""
    tops = _maximal(list(dict.fromkeys(mutant.outer for mutant in plan.mutants)))
    whole = _file_text(plan, tops)
    if _compiles(whole, plan.module.path):
        return Rewrite(plan.module.path, whole, ())
    good = [top for top in tops if _compiles(_file_text(plan, [top]), plan.module.path)]
    partial = _file_text(plan, good)
    kept = good if _compiles(partial, plan.module.path) else []
    failed = tuple(mutant.name for mutant in plan.mutants if not any(_contains(top, mutant.outer) for top in kept))
    return Rewrite(plan.module.path, _file_text(plan, kept), failed)


def _compiles(text: str, path: str) -> bool:
    try:
        compile(text, path, "exec", dont_inherit=True)
    except (SyntaxError, ValueError):
        return False
    return True


def _file_text(plan: Plan, tops: list[Span]) -> str:
    everything = Span(plan.module.prefix, plan.module.prefix + len(plan.data))
    patches = [Patch(top, _switch(top, plan)) for top in tops]
    return _spliced(everything, [plan.preamble, *patches], plan).decode(plan.module.encoding)


def _contains(outer: Span, inner: Span) -> bool:
    return outer.start <= inner.start and inner.end <= outer.end


def _maximal(spans: list[Span]) -> list[Span]:
    """The spans that lie inside no other span of the list."""
    return [span for span in spans if not any(other != span and _contains(other, span) for other in spans)]


def _switch(span: Span, plan: Plan) -> bytes:
    """The expression as a conditional: each of its own mutants when that mutant is on, else the original."""
    spans = list(dict.fromkeys(mutant.outer for mutant in plan.mutants))
    inside = [other for other in spans if other != span and _contains(span, other)]
    nested = [Patch(child, _switch(child, plan)) for child in _maximal(inside)]
    own = [mutant for mutant in plan.mutants if mutant.outer == span]
    variants = [_variant(mutant, span, nested, plan) for mutant in own]
    return b"(" + b"".join(variants) + b"(" + _spliced(span, nested, plan) + b"))"


def _variant(mutant: Mutant, span: Span, nested: list[Patch], plan: Plan) -> bytes:
    token = Patch(Span(mutant.piece.start, mutant.piece.end), mutation.replacement_of(mutant).encode(ENCODING))
    switch = f" if {MUTANT_MODULE}.on({plan.indexes[mutant.name]}) else ".encode(ENCODING)
    return b"(" + _spliced(span, [*nested, token], plan) + b")" + switch


def _spliced(span: Span, patches: list[Patch], plan: Plan) -> bytes:
    """The bytes of the span with each patch in place of the stretch it covers."""
    ordered = sorted(patches, key=_patch_order)
    shift = plan.module.prefix
    starts = [span.start, *(patch.span.end for patch in ordered)]
    ends = [*(patch.span.start for patch in ordered), span.end]
    kept = [plan.data[start - shift : end - shift] for start, end in zip(starts, ends)]
    return b"".join(part for pair in zip(kept, [*(patch.text for patch in ordered), b""]) for part in pair)


def _patch_order(patch: Patch) -> tuple[int, int]:
    return (patch.span.start, patch.span.end)
