/** Writes the three interchange files. The only module that writes to the file system. */
import * as fs from "node:fs";
import * as path from "node:path";
import { failure } from "../model/failure";
import type { Outputs } from "../model/inputs";

const INDENT = 2;

export function writeOutputs(directory: string, outputs: Outputs): void {
  try {
    fs.mkdirSync(directory, { recursive: true });
    writeJson(path.join(directory, "findings.sarif"), outputs.sarif);
    writeJson(path.join(directory, "graph.json"), outputs.graph);
    writeJson(path.join(directory, "units.json"), outputs.units);
  } catch (error) {
    throw failure(directory, `the output cannot be written (${error instanceof Error ? error.message : String(error)})`, "Check the path given as --out");
  }
}

function writeJson(file: string, document: unknown): void {
  fs.writeFileSync(file, `${JSON.stringify(document, undefined, INDENT)}\n`);
}
