/** Reads the inputs of an analysis. With the compiler and the writer, the only modules that touch the file system. */
import * as fs from "node:fs";
import * as path from "node:path";
import { failure } from "../model/failure";
import { sourcePaths } from "../analysis/project";
import type { Command, Inputs, PackageName, YamlText } from "../model/inputs";

const SKIPPED_DIRECTORIES: ReadonlySet<string> = new Set(["node_modules", "dist"]);
const GLOSSARY = ".gate/glossary.yaml";
const MANIFEST = "package.json";
const BYTE_ORDER_MARK = Buffer.from("\uFEFF", "utf8");

export function readInputs(command: Command): Inputs {
  const files = walk(command.root, "").sort();
  const glossary = path.join(command.root, GLOSSARY);
  return {
    root: command.root,
    config: readText(command.config, "--config"),
    architecture: readText(command.architecture, "--architecture"),
    glossary: fs.existsSync(glossary) ? readText(glossary, "the glossary") : undefined,
    lines: command.lines ? readText(command.lines, "--lines") : undefined,
    files,
    withByteOrderMark: sourcePaths(files).filter((file) => startsWithMark(path.join(command.root, file))),
    packages: files.filter((file) => path.posix.basename(file) === MANIFEST).map(
      (file) => packageName(command.root, file)),
  };
}

function readText(file: string, role: string): YamlText {
  try {
    return { file, text: fs.readFileSync(file, "utf8") };
  } catch (error) {
    throw failure(file, `the file cannot be read (${reasonOf(error)})`, `Check the path given as ${role}`);
  }
}

/** Whether the file's bytes start with the UTF-8 byte order mark. */
function startsWithMark(file: string): boolean {
  try {
    return fs.readFileSync(file).subarray(0, BYTE_ORDER_MARK.length).equals(BYTE_ORDER_MARK);
  } catch (error) {
    throw failure(file, `the file cannot be read (${reasonOf(error)})`, "Check that the source file is readable");
  }
}

function reasonOf(error: unknown): string {
  return error instanceof Error && "code" in error ? String(error.code) : String(error);
}

/** Every file under the root as a root-relative path with forward slashes. */
function walk(root: string, directory: string): string[] {
  const entries = listDirectory(path.join(root, directory));
  return entries.flatMap((entry) => {
    const relative = directory ? `${directory}/${entry.name}` : entry.name;
    if (!entry.isDirectory()) return [relative];
    const isSkipped = SKIPPED_DIRECTORIES.has(entry.name) || entry.name.startsWith(".");
    return isSkipped ? [] : walk(root, relative);
  });
}

function listDirectory(directory: string): fs.Dirent[] {
  try {
    return fs.readdirSync(directory, { withFileTypes: true });
  } catch (error) {
    throw failure(directory, `the directory cannot be read (${reasonOf(error)})`, "Check the path given as --root");
  }
}

function packageName(root: string, file: string): PackageName {
  const directory = path.posix.dirname(file);
  const manifest: unknown = parseJson(path.join(root, file));
  const name = typeof manifest === "object" && manifest && "name" in manifest ? String(manifest.name) : "-";
  return { directory: directory === "." ? "" : directory, name };
}

function parseJson(file: string): unknown {
  try {
    return JSON.parse(fs.readFileSync(file, "utf8"));
  } catch (error) {
    throw failure(file, `the file is not valid JSON (${error instanceof Error ? error.message : String(error)})`,
      "Fix or remove it");
  }
}
