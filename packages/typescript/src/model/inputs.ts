/** What the reader hands to the analysis and what the analysis hands to the writer. */
import type * as ts from "typescript";

export interface Command {
  root: string;
  out: string;
  config: string;
  architecture: string;
  today: string;
}

export interface YamlText {
  file: string;
  text: string;
}

export interface PackageName {
  directory: string;
  name: string;
}

export interface Inputs {
  root: string;
  config: YamlText;
  architecture: YamlText;
  glossary: YamlText | undefined;
  files: string[];
  packages: PackageName[];
}

export interface CompilerFacts {
  configPresent: boolean;
  strictNullChecks: boolean;
  configFirstLine: string;
}

export interface Compilation {
  program: ts.Program;
  facts: CompilerFacts;
}

export interface Outputs {
  sarif: unknown;
  graph: unknown;
  units: unknown;
  problems: string[];
}
