/** The command-line entry: reads the arguments, runs the analysis, reports on the console, sets the exit code. */
import { perform } from "../perform";
import { sourcePaths } from "../analysis/project";
import { parseCommand } from "../command";
import { Failure } from "../model/failure";
import { PACKAGE_VERSION, SPEC_VERSION, TOOL_NAME } from "../model/ruleTable";
import { compile } from "./compiler";
import { readInputs } from "./reader";
import { writeFiles } from "./writer";

const EXIT_ANALYZED = 0;
const EXIT_FAILED = 2;

export function main(argv: readonly string[]): void {
  try {
    process.exitCode = execute(argv);
  } catch (error) {
    console.error(`${TOOL_NAME}: ${describe(error)}`);
    process.exitCode = EXIT_FAILED;
  }
}

function execute(argv: readonly string[]): number {
  if (argv[0] === "--version") {
    console.log(`${PACKAGE_VERSION} spec ${SPEC_VERSION}`);
    return EXIT_ANALYZED;
  }
  const command = parseCommand(argv, process.cwd());
  const inputs = readInputs(command);
  const outputs = perform(command, inputs, compile(inputs.root, sourcePaths(inputs.files)));
  writeFiles(outputs.files);
  outputs.problems.forEach((problem) => console.error(`${TOOL_NAME}: ${problem}`));
  return outputs.problems.length > 0 ? EXIT_FAILED : EXIT_ANALYZED;
}

/** A failure carries its own readable line; anything else is a defect of the package and is said to be one. */
function describe(error: unknown): string {
  if (error instanceof Failure) return error.message;
  const detail = error instanceof Error ? error.message : String(error);
  return `internal error: ${detail}. Report it with the command line you ran; this is a defect in ${TOOL_NAME}.`;
}
