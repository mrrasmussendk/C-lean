/** Compiles the sources into one program. The TypeScript compiler reads the files and the root's tsconfig.json. */
import * as fs from "node:fs";
import * as path from "node:path";
import * as ts from "typescript";
import { failure } from "../model/failure";
import type { Compilation } from "../model/inputs";

const CONFIG_NAME = "tsconfig.json";

/** Used when the root holds no tsconfig.json. No ambient type packages, so results do not depend on the machine. */
const DEFAULT_OPTIONS: ts.CompilerOptions = {
  strict: true,
  target: ts.ScriptTarget.ES2022,
  module: ts.ModuleKind.ESNext,
  moduleResolution: ts.ModuleResolutionKind.Bundler,
  jsx: ts.JsxEmit.Preserve,
  types: [],
};

/** Null analysis needs strict null semantics whatever the project says; ER02 reports a project that turns them off. */
const FORCED_OPTIONS: ts.CompilerOptions = { strictNullChecks: true, noEmit: true, skipLibCheck: true };

export function compile(root: string, sources: readonly string[]): Compilation {
  const configPath = path.join(root, CONFIG_NAME);
  const configPresent = fs.existsSync(configPath);
  const options = configPresent ? configuredOptions(root, configPath) : DEFAULT_OPTIONS;
  const rootNames = sources.map((source) => path.join(root, source));
  return {
    program: ts.createProgram({ rootNames, options: { ...options, ...FORCED_OPTIONS } }),
    facts: {
      configPresent,
      strictNullChecks: options.strictNullChecks ?? options.strict ?? false,
      configFirstLine: configPresent ? fs.readFileSync(configPath, "utf8").split("\n")[0] : "",
      importExtension: importExtensionOf(options),
    },
  };
}

/** `.ts` when the project imports TypeScript files by their own name, `.js` under Node's ESM resolution, else none. */
function importExtensionOf(options: ts.CompilerOptions): string {
  if (options.allowImportingTsExtensions) return ".ts";
  const module = options.module ?? ts.ModuleKind.None;
  return module >= ts.ModuleKind.Node16 && module <= ts.ModuleKind.NodeNext ? ".js" : "";
}

function configuredOptions(root: string, configPath: string): ts.CompilerOptions {
  const loaded = ts.readConfigFile(configPath, ts.sys.readFile);
  if (loaded.error) throw configFailure(loaded.error);
  const parsed = ts.parseJsonConfigFileContent(loaded.config, ts.sys, root, undefined, configPath);
  const broken =
    parsed.errors.find((error) => error.category === ts.DiagnosticCategory.Error && error.code !== NO_INPUTS);
  if (broken) throw configFailure(broken);
  return parsed.options;
}

/** "No inputs were found in config file": the analyzer lists the sources itself. */
const NO_INPUTS = 18003;

function configFailure(diagnostic: ts.Diagnostic): Error {
  const file = diagnostic.file;
  const at = file ? file.getLineAndCharacterOfPosition(diagnostic.start ?? 0) : { line: 0, character: 0 };
  const place = `${file?.fileName ?? CONFIG_NAME}:${at.line + 1}:${at.character + 1}`;
  const message = ts.flattenDiagnosticMessageText(diagnostic.messageText, " ").replace(/\.$/, "");
  return failure(place, message, "Fix tsconfig.json");
}
