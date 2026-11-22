"""The analysis from loaded inputs to the three output documents. Pure."""

from gate_python import build, configuration, graph, runner, sarif, units
from gate_python.model import Analysis, Inputs, Outputs


def outputs_of(inputs: Inputs) -> Outputs:
    config = configuration.config_of(inputs)
    built = build.build(inputs.sources, config)
    analysis = Analysis(built.program, config, inputs.projects)
    edges = graph.edges_of(built.program)
    return Outputs(
        sarif=sarif.document(runner.results_of(analysis), analysis),
        graph=graph.document(analysis, edges),
        units=units.document(analysis, edges),
        problems=built.problems,
    )
