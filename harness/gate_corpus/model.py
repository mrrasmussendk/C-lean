"""Data the harness works on. No behaviour beyond construction."""

from dataclasses import dataclass


@dataclass(frozen=True)
class Case:
    suite: str
    case_id: str
    kind: str
    rules: tuple[str, ...]


@dataclass(frozen=True)
class Finding:
    rule: str
    file: str
    line: int


@dataclass(frozen=True)
class Edge:
    source: str
    target: str
    kind: str


@dataclass(frozen=True)
class Node:
    symbol: str
    kind: str
    component: str


@dataclass(frozen=True)
class Graph:
    nodes: frozenset[Node]
    edges: frozenset[Edge]


@dataclass(frozen=True)
class Outcome:
    case: Case
    problems: tuple[str, ...]
