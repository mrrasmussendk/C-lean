"""The three commands from loaded inputs to what must be written. Pure."""

from gate_python import build, configuration, graph, instrumenting, mutation, runner, sarif, switching, units
from gate_python.model import Analysis, Inputs, Outputs
from gate_python.runtime_names import HELPER_FILE, HELPER_FILES, MUTANT_FILE


def outputs_of(inputs: Inputs) -> Outputs:
    config = configuration.config_of(inputs)
    built = build.build(inputs.sources, config)
    analysis = Analysis(built.program, config, inputs.projects)
    edges = graph.edges_of(built.program)
    documents = {
        "findings.sarif": sarif.document(runner.results_of(analysis), analysis),
        "graph.json": graph.document(analysis, edges),
        "units.json": units.document(analysis, edges),
    }
    return Outputs(documents, {}, built.problems)


def mutants_of(inputs: Inputs) -> Outputs:
    config = configuration.config_of(inputs)
    built = build.build(inputs.sources, config)
    mutants = mutation.found(built.program, configuration.line_ranges(inputs.line_filters))
    return Outputs({"mutants.json": mutation.document(mutants, None)}, {}, built.problems)


def switched_mutants(inputs: Inputs) -> Outputs:
    """The mutants, and every production file that holds one rewritten with each mutant behind a switch."""
    config = configuration.config_of(inputs)
    built = build.build(inputs.sources, config)
    mutants = mutation.found(built.program, configuration.line_ranges(inputs.line_filters))
    rewrites = switching.switched_files(built.program, mutants, inputs.root)
    failed = frozenset(name for rewrite in rewrites for name in rewrite.failed)
    sources = {rewrite.path: rewrite.text for rewrite in rewrites}
    helper = switching.helper_text(inputs.mutant_helper, [mutant.name for mutant in mutants])
    return Outputs(
        {"mutants.json": mutation.document(mutants, failed)}, sources | {MUTANT_FILE: helper}, built.problems
    )


def instrumented(inputs: Inputs, trace_file: str) -> Outputs:
    """Every production file rewritten to record component calls, plus the runtime helper."""
    config = configuration.config_of(inputs)
    built = build.build(inputs.sources, config)
    production = [
        module for module in built.program.modules.values() if not module.test and module.path not in HELPER_FILES
    ]
    texts = {module.path: instrumenting.rewritten(module, inputs.root) for module in production}
    helper = instrumenting.helper_text(inputs.helper, trace_file)
    return Outputs({}, texts | {HELPER_FILE: helper}, built.problems)
