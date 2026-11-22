"""The three commands from loaded inputs to what must be written. Pure."""

from gate_python import build, configuration, graph, instrumenting, mutation, runner, sarif, units
from gate_python.model import Analysis, Inputs, Outputs
from gate_python.runtime_names import HELPER_FILE


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
    wanted = configuration.line_ranges(inputs.line_filters)
    return Outputs({"mutants.json": mutation.document(built.program, wanted)}, {}, built.problems)


def instrumented(inputs: Inputs, trace_file: str) -> Outputs:
    """Every production file rewritten to record component calls, plus the runtime helper."""
    config = configuration.config_of(inputs)
    built = build.build(inputs.sources, config)
    production = [module for module in built.program.modules.values() if not module.test and module.path != HELPER_FILE]
    texts = {module.path: instrumenting.rewritten(module, inputs.root) for module in production}
    helper = instrumenting.helper_text(inputs.helper, trace_file)
    return Outputs({}, texts | {HELPER_FILE: helper}, built.problems)
