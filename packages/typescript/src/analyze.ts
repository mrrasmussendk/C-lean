/** The analysis as one pure step: inputs and a compiled program in, the three documents out. */
import { graphOf } from "./analysis/graph";
import { judge } from "./analysis/judge";
import { buildProject, parseProblems } from "./analysis/project";
import { unitRecords } from "./analysis/unitRecords";
import type { Compilation, Inputs, Outputs } from "./model/inputs";
import { graphDocument, sarifDocument, unitsDocument } from "./output/documents";

export function analyze(inputs: Inputs, compilation: Compilation, today: string): Outputs {
  const project = buildProject(inputs, compilation, today);
  const graph = graphOf(project);
  return {
    sarif: sarifDocument(judge(project)),
    graph: graphDocument(graph),
    units: unitsDocument(unitRecords(project, graph)),
    problems: parseProblems(inputs, compilation.program),
  };
}
