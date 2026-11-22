"""Data the analyzer works on. No behaviour beyond construction."""

import ast
from dataclasses import dataclass
from typing import Any, Mapping

FunctionNode = ast.FunctionDef | ast.AsyncFunctionDef | ast.Lambda

FUNCTION = "function"
CLASS = "class"
EXTERNAL = "external"
VALUE = "value"
UNRESOLVED = "unresolved"
UNTYPED = "untyped"


@dataclass(frozen=True)
class Problem:
    """Why the package could not do its work: where, what is wrong and what to do."""

    file: str
    line: int
    column: int
    text: str


@dataclass
class InputError(Exception):
    problem: Problem


@dataclass(frozen=True)
class Options:
    root: str
    out: str
    config: str
    architecture: str
    today: str


@dataclass(frozen=True)
class SourceFile:
    path: str
    text: str


@dataclass(frozen=True)
class Inputs:
    sources: tuple[SourceFile, ...]
    config: SourceFile
    architecture: SourceFile
    glossaries: tuple[SourceFile, ...]
    projects: tuple[SourceFile, ...]
    today: str


@dataclass(frozen=True)
class Component:
    name: str
    paths: tuple[str, ...]
    effect: bool


@dataclass(frozen=True)
class Config:
    tests: tuple[str, ...]
    generated: tuple[str, ...]
    limits: Mapping[str, int]
    severities: Mapping[str, str]
    components: tuple[Component, ...]
    banned: Mapping[str, str]
    today: str


@dataclass(frozen=True)
class Comment:
    line: int
    text: str
    alone: bool


@dataclass(frozen=True)
class Module:
    path: str
    name: str
    tree: ast.Module
    lines: tuple[str, ...]
    comments: tuple[Comment, ...]
    code_lines: frozenset[int]
    test: bool
    component: str
    effect: bool


@dataclass(frozen=True)
class TypeRef:
    """A type by qualified name; target is the function, class or module a name stands for."""

    name: str
    args: tuple["TypeRef", ...] = ()
    target: str = ""


@dataclass(frozen=True)
class Target:
    kind: str
    name: str


@dataclass(frozen=True)
class FunctionInfo:
    symbol: str
    qualified: str
    name: str
    path: str
    node: FunctionNode
    owner: str
    parent: str
    key: str


@dataclass(frozen=True)
class ClassInfo:
    symbol: str
    qualified: str
    path: str
    node: ast.ClassDef
    bases: tuple[str, ...]


@dataclass(frozen=True)
class Program:
    modules: Mapping[str, Module]
    paths: Mapping[str, str]
    namespaces: frozenset[str]
    bindings: Mapping[str, Mapping[str, str]]
    functions: Mapping[str, FunctionInfo]
    all_functions: tuple[FunctionInfo, ...]
    classes: Mapping[str, ClassInfo]
    fields: Mapping[str, Mapping[str, TypeRef]]
    envs: Mapping[str, Mapping[str, TypeRef]]


@dataclass(frozen=True)
class Scope:
    program: Program
    module: Module
    env: Mapping[str, TypeRef]


@dataclass(frozen=True)
class Located:
    node: ast.AST
    scope: Scope


@dataclass(frozen=True)
class Analysis:
    program: Program
    config: Config
    projects: tuple[SourceFile, ...]


@dataclass(frozen=True)
class Finding:
    rule: str
    file: str
    line: int
    message: str
    anchor: str


@dataclass(frozen=True)
class Result:
    finding: Finding
    suppressed: bool


@dataclass(frozen=True)
class Edge:
    source: str
    target: str
    kind: str


@dataclass(frozen=True)
class Outputs:
    sarif: Mapping[str, Any]
    graph: Mapping[str, Any]
    units: Mapping[str, Any]
    problems: tuple[Problem, ...]
