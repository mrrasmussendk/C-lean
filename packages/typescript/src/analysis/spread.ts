/** Shared by the design rules: which production function holds a node, and how to list functions in a message. */
import type * as ts from "typescript";
import type { FunctionUnit, Project } from "../model/project";
import { productionFunctions } from "./report";
import { ancestors } from "./syntax";

/** Functions in (file, line) order. */
export function inFileOrder(functions: readonly FunctionUnit[]): FunctionUnit[] {
  const key = (fn: FunctionUnit): string => `${fn.path} ${String(fn.line).padStart(LINE_DIGITS, "0")}`;
  return [...functions].sort((left, right) => (key(left) < key(right) ? -1 : Number(key(left) > key(right))));
}

const LINE_DIGITS = 9;

/** `total at src/orders.ts:12, ...` for every function, in (file, line) order. */
export function listed(functions: readonly FunctionUnit[]): string {
  return inFileOrder(functions)
    .map((fn) => `${fn.name} at ${fn.path}:${fn.line}`)
    .join(", ");
}

/** Finds the production function that directly holds a node. */
export function functionFinder(project: Project): (node: ts.Node) => FunctionUnit | undefined {
  const units = new Map(productionFunctions(project).map((fn) => [fn.node as ts.Node, fn] as const));
  return (node) => ancestors(node).flatMap((ancestor) => units.get(ancestor) ?? [])[0];
}
