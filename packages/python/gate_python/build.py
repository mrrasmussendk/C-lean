"""Builds the Program: parsed modules, symbol tables, field types and local types."""

from dataclasses import dataclass, replace

from gate_python import bindings, environment, fields, parsing, symbols
from gate_python.model import ClassInfo, Config, FunctionInfo, Module, Problem, Program, SourceFile


@dataclass(frozen=True)
class Built:
    program: Program
    problems: tuple[Problem, ...]


def build(sources: tuple[SourceFile, ...], config: Config) -> Built:
    wanted = [source for source in sources if not parsing.is_generated(source, config)]
    parsed = [parsing.module_of(source, config) for source in wanted]
    modules = [module for module in parsed if isinstance(module, Module)]
    problems = tuple(problem for problem in parsed if isinstance(problem, Problem))
    return Built(_typed(_tables(modules)), problems)


def _tables(modules: list[Module]) -> Program:
    tables = {module.path: bindings.module_bindings(module) for module in modules}
    declared = [item for module in modules for item in _declarations(module, tables[module.path])]
    functions = [item for item in declared if isinstance(item, FunctionInfo)]
    classes = [item for item in declared if isinstance(item, ClassInfo)]
    return Program(
        modules={module.path: module for module in modules},
        paths={module.name: module.path for module in modules},
        namespaces=frozenset(name for module in modules for name in _prefixes(module.name)),
        bindings=tables,
        functions={function.qualified: function for function in reversed(functions)},
        all_functions=tuple(functions),
        classes={info.qualified: info for info in reversed(classes)},
        fields={},
        envs={},
    )


def _declarations(module: Module, table: dict[str, str]) -> list[FunctionInfo | ClassInfo]:
    return symbols.declarations(module.tree, symbols.Place(module, table, module.name, ""))


def _prefixes(name: str) -> list[str]:
    parts = name.split(".")
    return [".".join(parts[:size]) for size in range(1, len(parts) + 1)]


def _typed(program: Program) -> Program:
    with_fields = replace(program, fields=fields.field_types(program))
    return replace(with_fields, envs=environment.environments(with_fields))
