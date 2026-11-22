/** Text edits on one source text, and the import of a helper module that rewritten code needs. */
import * as path from "node:path";
import * as ts from "typescript";
import type { Project, SourceUnit } from "../model/project";

/**
 * Text to put at a position, after removing `remove` characters there.
 * Where several meet, the ones that close come first, innermost first.
 */
export interface Edit {
  at: number;
  remove: number;
  text: string;
  closes: boolean;
  depth: number;
}

/** The text with every edit applied. Edits must not overlap. */
export function applyEdits(text: string, edits: readonly Edit[]): string {
  const ordered = [...edits].sort(inOrder);
  const ends = [...ordered.slice(1).map((edit) => edit.at), text.length];
  const pieces = ordered.map((edit, index) => edit.text + text.slice(edit.at + edit.remove, ends[index]));
  return text.slice(0, ordered[0]?.at ?? text.length) + pieces.join("");
}

function inOrder(left: Edit, right: Edit): number {
  if (left.at !== right.at) return left.at - right.at;
  if (left.closes !== right.closes) return left.closes ? -1 : 1;
  return left.closes ? right.depth - left.depth : left.depth - right.depth;
}

export interface HelperPlace {
  /** Root-relative path of the helper file. */
  file: string;
  /** The module specifier that reaches the helper from a given source file. */
  specifierFrom: (unit: SourceUnit) => string;
}

/** The helper goes in the deepest directory that holds every rewritten file: inside the project's source root. */
export function placeHelper(project: Project, units: readonly SourceUnit[], name: string): HelperPlace {
  const directory = commonDirectory(units.map((unit) => path.posix.dirname(unit.path)));
  const extension = importExtension(project, units);
  return {
    file: path.posix.join(directory, `${name}.ts`),
    specifierFrom: (unit) => {
      const relative = path.posix.relative(path.posix.dirname(unit.path), path.posix.join(directory, name));
      return `${relative.startsWith(".") ? relative : `./${relative}`}${extension}`;
    },
  };
}

function commonDirectory(directories: readonly string[]): string {
  const split = directories.map((directory) => (directory === "." ? [] : directory.split("/")));
  const first = split[0] ?? [];
  const shared =
    first.filter(
      (_, index) =>
        split.every((parts) => parts.slice(0, index + 1).join("/") === first.slice(0, index + 1).join("/")));
  return shared.join("/");
}

/** The extension the project's own relative imports carry; the compiler options decide when it has none. */
function importExtension(project: Project, units: readonly SourceUnit[]): string {
  const specifiers = units
    .flatMap((unit) => unit.source.statements.filter(ts.isImportDeclaration))
    .flatMap(
      (declaration) => (ts.isStringLiteral(declaration.moduleSpecifier) ? [declaration.moduleSpecifier.text] : []))
    .filter((specifier) => specifier.startsWith("."));
  const written = [".js", ".ts"].find((extension) => specifiers.some((specifier) => specifier.endsWith(extension)));
  return written ?? (specifiers.length > 0 ? "" : project.compiler.importExtension);
}
