/** Parses the command line. Pure: the working directory is passed in. */
import * as path from "node:path";
import { failure } from "./model/failure";
import type { Command } from "./model/inputs";

const USAGE = [
  "Run it as one of: gate-typescript analyze --root <dir> --out <dir> --config <gate.yaml> --architecture <architecture.yaml> --today <YYYY-MM-DD>",
  "gate-typescript mutants --root <dir> --out <dir> --config <gate.yaml> --architecture <architecture.yaml> [--lines <file>] [--switch]",
  "gate-typescript instrument --root <dir> --config <gate.yaml> --architecture <architecture.yaml> --trace-file <absolute path>",
].join("; ");
const OPTION_PREFIX = "--";
const PAIR = 2;

/** The options each command requires, and after them the ones it merely accepts. */
const REQUIRED: ReadonlyMap<string, readonly string[]> = new Map([
  ["analyze", ["root", "out", "config", "architecture", "today"]],
  ["mutants", ["root", "out", "config", "architecture"]],
  ["instrument", ["root", "config", "architecture", "trace-file"]],
]);
/** Accepted by mutants and without effect: tests run from source here, so mutants are not compiled in behind a switch. */
const SWITCH_FLAG = "--switch";
const OPTIONAL: ReadonlyMap<string, readonly string[]> = new Map([["mutants", ["lines"]]]);

export function parseCommand(argv: readonly string[], cwd: string): Command {
  const name = argv[0] ?? "(none)";
  const required = REQUIRED.get(name);
  if (!required) throw failure("", `unknown command ${name}`, USAGE);
  if (argv.includes(SWITCH_FLAG) && name !== "mutants") throw failure("", `${name} does not take ${SWITCH_FLAG}`, USAGE);
  const options = optionsOf(argv.slice(1).filter((word) => word !== SWITCH_FLAG));
  const missing = required.find((option) => !options.get(option));
  if (missing) throw failure("", `missing argument --${missing} of ${name}`, USAGE);
  const accepted = [...required, ...(OPTIONAL.get(name) ?? [])];
  const stray = [...options.keys()].find((option) => !accepted.includes(option));
  if (stray !== undefined) throw failure("", `${name} does not take --${stray}`, USAGE);
  const pathOf = (option: string): string => (options.get(option) ? path.resolve(cwd, options.get(option) ?? "") : "");
  return {
    name,
    root: pathOf("root"),
    out: pathOf("out"),
    config: pathOf("config"),
    architecture: pathOf("architecture"),
    today: dateOf(options.get("today")),
    lines: pathOf("lines"),
    traceFile: pathOf("trace-file"),
  };
}

function optionsOf(words: readonly string[]): ReadonlyMap<string, string> {
  const names = words.filter((_, index) => index % PAIR === 0);
  const stray = names.find((name) => !name.startsWith(OPTION_PREFIX));
  if (stray !== undefined) throw failure("", `unexpected argument ${stray}`, USAGE);
  return new Map(names.map((name, index) => [name.slice(OPTION_PREFIX.length), words[index * PAIR + 1] ?? ""]));
}

function dateOf(text: string | undefined): string {
  if (text === undefined) return "";
  if (!/^\d{4}-\d{2}-\d{2}$/.test(text)) throw failure("", `--today is ${text}, which is not a date`, "Give it as YYYY-MM-DD");
  return text;
}
