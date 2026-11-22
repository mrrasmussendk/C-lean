/** Builds findings. The wording is `<what is wrong, with measured value and limit>. <what to do>`. */
import type * as ts from "typescript";
import type { Finding } from "../model/finding";
import type { FunctionUnit, Project, SourceUnit, TypeUnit } from "../model/project";
import { sha256 } from "./digest";
import { startLine } from "./syntax";

type Place = Pick<SourceUnit, "path" | "source">;
type Symbolic = Pick<FunctionUnit | TypeUnit, "path" | "line" | "id">;

/** A finding on a function or type, on the line of its name. */
export function onSymbol(rule: string, symbol: Symbolic, wording: string): Finding {
  return { rule, file: symbol.path, line: symbol.line, message: `${rule}: ${wording}.`, anchor: symbol.id };
}

/** A finding on a parameter of a function, on the parameter's line. */
export function onParameter(rule: string, fn: FunctionUnit, parameter: ts.ParameterDeclaration, wording: string): Finding {
  return { ...onSymbol(rule, fn, wording), line: startLine(parameter.name) };
}

/** A finding on a source line that is not a symbol. */
export function onLine(rule: string, place: Place, line: number, wording: string): Finding {
  const text = place.source.text.split("\n")[line - 1] ?? "";
  return { rule, file: place.path, line, message: `${rule}: ${wording}.`, anchor: sha256(text.trim()) };
}

/** A finding on the line where a construct starts. */
export function onNode(rule: string, path: string, node: ts.Node, wording: string): Finding {
  const place = { path, source: node.getSourceFile() };
  return onLine(rule, place, startLine(node), wording);
}

export function productionFiles(project: Project): SourceUnit[] {
  return project.files.filter((unit) => !unit.test);
}

export function testFiles(project: Project): SourceUnit[] {
  return project.files.filter((unit) => unit.test);
}

export function productionFunctions(project: Project): FunctionUnit[] {
  return productionFiles(project).flatMap((unit) => unit.functions);
}

export function productionTypes(project: Project): TypeUnit[] {
  return productionFiles(project).flatMap((unit) => unit.types);
}
