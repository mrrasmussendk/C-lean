// FX02: Command-query separation: a function that returns a value must not assign to instance, static or module state
import * as ts from "typescript";
import { onSymbol, productionFunctions } from "../analysis/report";
import { stateWrites } from "../analysis/state";
import { ownDescendants } from "../analysis/syntax";
import type { Finding } from "../model/finding";
import type { FunctionNode, Project } from "../model/project";

export function FX02(project: Project): Finding[] {
  return productionFunctions(project)
    .filter((fn) => returnsValue(fn.node))
    .map((fn) => ({ fn, writes: stateWrites(ownDescendants(fn.node), project.checker) }))
    .filter((measured) => measured.writes.length > 0)
    .map((measured) =>
      onSymbol("FX02", measured.fn, `function ${measured.fn.name} returns a value and assigns state ${measured.writes[0].getText()}. Split it into a query that returns and a command that assigns`),
    );
}

/** A constructor and a setter return nothing; an arrow function with an expression body returns that expression. */
function returnsValue(fn: FunctionNode): boolean {
  if (ts.isConstructorDeclaration(fn) || ts.isSetAccessorDeclaration(fn)) return false;
  if (!ts.isBlock(fn.body)) return true;
  return ownDescendants(fn.body).some((node) => ts.isReturnStatement(node) && node.expression !== undefined);
}
