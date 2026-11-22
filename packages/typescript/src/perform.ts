/** Each command as one pure step: inputs and a compiled program in, the files to write out. */
import * as path from "node:path";
import { graphOf } from "./analysis/graph";
import { instrumentedSources } from "./analysis/instrument";
import { judge } from "./analysis/judge";
import { mutantsOf } from "./analysis/mutants";
import { buildProject, parseProblems } from "./analysis/project";
import { parseLineRanges } from "./analysis/settings";
import { switchedSources } from "./analysis/switching";
import { unitRecords } from "./analysis/unitRecords";
import type { Command, Compilation, Inputs, OutputFile, Outputs } from "./model/inputs";
import type { Project } from "./model/project";
import { graphDocument, mutantsDocument, sarifDocument, unitsDocument } from "./output/documents";

const INDENT = 2;

export function perform(command: Command, inputs: Inputs, compilation: Compilation): Outputs {
  const project = buildProject(inputs, compilation, command.today);
  return { files: filesOf(command, inputs, project), problems: parseProblems(inputs, compilation.program) };
}

function filesOf(command: Command, inputs: Inputs, project: Project): OutputFile[] {
  if (command.name === "instrument") {
    return instrumentedSources(project, command.traceFile).map((source) => ({ ...source, path: path.join(command.root, source.path) }));
  }
  if (command.name === "mutants") return mutantFiles(command, inputs, project);
  const graph = graphOf(project);
  return [
    jsonFile(command.out, "findings.sarif", sarifDocument(judge(project))),
    jsonFile(command.out, "graph.json", graphDocument(graph)),
    jsonFile(command.out, "units.json", unitsDocument(unitRecords(project, graph))),
  ];
}

/** mutants.json, and with --switch the rewritten sources and their helper as well. */
function mutantFiles(command: Command, inputs: Inputs, project: Project): OutputFile[] {
  const placed = mutantsOf(project, parseLineRanges(inputs.lines), new Set(inputs.withByteOrderMark));
  if (!command.switched) return [jsonFile(command.out, "mutants.json", mutantsDocument(placed.map((entry) => entry.mutant), false))];
  const switching = switchedSources(project, placed);
  const sources = switching.files.map((source) => ({ ...source, path: path.join(command.root, source.path) }));
  return [jsonFile(command.out, "mutants.json", mutantsDocument(switching.mutants, true)), ...sources];
}

function jsonFile(directory: string, name: string, document: unknown): OutputFile {
  return { path: path.join(directory, name), text: `${JSON.stringify(document, undefined, INDENT)}\n` };
}
