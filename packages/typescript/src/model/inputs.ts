/** What the reader hands to the analysis and what the analysis hands to the writer. */
import type * as ts from "typescript";

/** A parsed command line. Options a command does not take are empty. */
export interface Command {
  name: string;
  root: string;
  out: string;
  config: string;
  architecture: string;
  today: string;
  lines: string;
  traceFile: string;
  /** Whether `mutants` also compiles every mutant in behind a switch. */
  switched: boolean;
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
  lines: YamlText | undefined;
  files: string[];
  /** Sources stored with a UTF-8 byte order mark, which the compiler drops from the text it hands out. */
  withByteOrderMark: string[];
  packages: PackageName[];
}

export interface CompilerFacts {
  configPresent: boolean;
  strictNullChecks: boolean;
  configFirstLine: string;
  /** The extension the project's module resolution wants on a relative import: `.js`, `.ts` or none. */
  importExtension: string;
}

export interface Compilation {
  program: ts.Program;
  facts: CompilerFacts;
}

export interface OutputFile {
  path: string;
  text: string;
}

export interface Outputs {
  files: OutputFile[];
  problems: string[];
}
