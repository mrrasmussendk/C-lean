// ER03: A function with side effects must not signal failure through a boolean or integer return value
import * as ts from "typescript";
import { onSymbol, productionFunctions } from "../analysis/report";
import { stateWrites } from "../analysis/state";
import { bare, ownDescendants } from "../analysis/syntax";
import { everyFlag, returnedType } from "../analysis/types";
import type { Finding } from "../model/finding";
import type { FunctionNode, FunctionUnit, Project } from "../model/project";

export function ER03(project: Project): Finding[] {
  return productionFunctions(project)
    .filter((fn) => hasSideEffects(fn, project.checker))
    .flatMap((fn) => failureSignals(fn.node, project.checker).slice(0, 1).map((signal) =>
      onSymbol("ER03", fn, `function ${fn.name} has side effects and signals failure by returning ${signal}. Throw an error or return a result object that says what failed`),
    ));
}

/** The function directly calls an effect API or assigns instance, static or module state. */
function hasSideEffects(fn: FunctionUnit, checker: ts.TypeChecker): boolean {
  return fn.effects.length > 0 || stateWrites(ownDescendants(fn.node), checker).length > 0;
}

/** `false` returned from a boolean function, or a negative literal returned from a numeric one. */
function failureSignals(fn: FunctionNode, checker: ts.TypeChecker): string[] {
  const returned = returnedType(checker, fn);
  if (!returned) return [];
  const values = ownDescendants(fn.body).flatMap((node) => (ts.isReturnStatement(node) && node.expression ? [bare(node.expression)] : []));
  if (everyFlag(returned, ts.TypeFlags.BooleanLike)) return values.filter((value) => value.kind === ts.SyntaxKind.FalseKeyword).map(() => "the literal false");
  if (!everyFlag(returned, ts.TypeFlags.NumberLike)) return [];
  return values.filter(isNegativeLiteral).map((value) => `the literal ${value.getText()}`);
}

function isNegativeLiteral(value: ts.Expression): boolean {
  return ts.isPrefixUnaryExpression(value) && value.operator === ts.SyntaxKind.MinusToken && ts.isNumericLiteral(value.operand);
}
