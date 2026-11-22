/** Finds where a function writes into an object it received as a parameter. */
import * as ts from "typescript";
import type { FunctionNode } from "../model/project";
import { isMemberTarget, writeTarget } from "./state";
import { accessRoot, descendants } from "./syntax";
import { ownDeclarations } from "./types";

/** Standard-library methods that change the collection they are called on. */
const MUTATORS: ReadonlySet<string> = new Set([
  "push", "pop", "shift", "unshift", "splice", "sort", "reverse", "fill", "copyWithin", "set", "add", "delete", "clear",
]);

export interface ParameterWrite {
  parameter: ts.ParameterDeclaration;
  site: ts.Node;
}

/** Member assignments, element assignments and collection mutator calls on the function's own parameters. */
export function parameterWrites(checker: ts.TypeChecker, fn: FunctionNode): ParameterWrite[] {
  return descendants(fn.body).flatMap((site) => {
    const written = writtenObject(site, checker);
    const parameter = written && parameterOf(checker, fn, written);
    return parameter ? [{ parameter, site }] : [];
  });
}

/** The root of the object a node writes into, or undefined when the node writes into none. */
function writtenObject(node: ts.Node, checker: ts.TypeChecker): ts.Expression | undefined {
  const target = writeTarget(node);
  if (target) return isMemberTarget(target) ? accessRoot(target) : undefined;
  if (!ts.isCallExpression(node) || !ts.isPropertyAccessExpression(node.expression)) return undefined;
  const receiver = node.expression.expression;
  const isMutator = MUTATORS.has(node.expression.name.text) && !isDefinedHere(checker, receiver);
  return isMutator ? accessRoot(receiver) : undefined;
}

/** Whether the receiver's type is written under the root; what its methods do is then not decided here. */
function isDefinedHere(checker: ts.TypeChecker, receiver: ts.Expression): boolean {
  return ownDeclarations(checker.getTypeAtLocation(receiver)).some((declaration) => ts.isClassLike(declaration) || ts.isInterfaceDeclaration(declaration));
}

function parameterOf(checker: ts.TypeChecker, fn: FunctionNode, root: ts.Expression): ts.ParameterDeclaration | undefined {
  if (!ts.isIdentifier(root)) return undefined;
  const declaration = checker.getSymbolAtLocation(root)?.valueDeclaration;
  return declaration && ts.isParameter(declaration) && declaration.parent === fn ? declaration : undefined;
}
