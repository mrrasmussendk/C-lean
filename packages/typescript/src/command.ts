/** Parses the command line. Pure: the working directory is passed in. */
import * as path from "node:path";
import { failure } from "./model/failure";
import type { Command } from "./model/inputs";

const USAGE = "Run it as: gate-typescript analyze --root <dir> --out <dir> --config <gate.yaml> --architecture <architecture.yaml> --today <YYYY-MM-DD>";
const OPTION_PREFIX = "--";
const PAIR = 2;

export function parseCommand(argv: readonly string[], cwd: string): Command {
  if (argv[0] !== "analyze") throw failure("", `unknown command ${argv[0] ?? "(none)"}`, USAGE);
  const options = optionsOf(argv.slice(1));
  return {
    root: path.resolve(cwd, required(options, "root")),
    out: path.resolve(cwd, required(options, "out")),
    config: path.resolve(cwd, required(options, "config")),
    architecture: path.resolve(cwd, required(options, "architecture")),
    today: dateOf(required(options, "today")),
  };
}

function optionsOf(words: readonly string[]): ReadonlyMap<string, string> {
  const names = words.filter((_, index) => index % PAIR === 0);
  const stray = names.find((name) => !name.startsWith(OPTION_PREFIX));
  if (stray !== undefined) throw failure("", `unexpected argument ${stray}`, USAGE);
  return new Map(names.map((name, index) => [name.slice(OPTION_PREFIX.length), words[index * PAIR + 1] ?? ""]));
}

function required(options: ReadonlyMap<string, string>, name: string): string {
  const value = options.get(name);
  if (!value) throw failure("", `missing argument --${name}`, USAGE);
  return value;
}

function dateOf(text: string): string {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(text)) throw failure("", `--today is ${text}, which is not a date`, "Give it as YYYY-MM-DD");
  return text;
}
