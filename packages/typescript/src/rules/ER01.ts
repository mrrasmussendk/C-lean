// ER01: A catch block must not be empty and must not only log
import * as ts from "typescript";
import { isLogCall } from "../analysis/logging";
import { onNode, productionFiles } from "../analysis/report";
import { writeTarget } from "../analysis/state";
import { awaited, bare, descendants } from "../analysis/syntax";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const ADVICE = "Rethrow, wrap, or recover with a value the caller can check";

export function ER01(project: Project): Finding[] {
  return productionFiles(project).flatMap((unit) =>
    descendants(unit.source)
      .filter(ts.isCatchClause)
      .filter((clause) =>
        clause.block.statements.every(
          (statement) => isLogStatement(statement) || onlyLeaves(statement) || doesNothing(statement)))
      .map((clause) =>
        onNode("ER01", unit.path, clause,
          `catch block has ${clause.block.statements.length} statements and none handles the error. ${ADVICE}`),
      ),
  );
}

/** continue, break, a bare return, or a return of undefined or null: control leaves and no value says what happened. */
function onlyLeaves(statement: ts.Statement): boolean {
  if (ts.isContinueStatement(statement) || ts.isBreakStatement(statement)) return true;
  if (!ts.isReturnStatement(statement)) return false;
  const value = statement.expression ? bare(statement.expression) : undefined;
  return value === undefined || value.kind === ts.SyntaxKind.NullKeyword || value.getText() === "undefined";
}

/** An empty statement, or an expression that calls, creates, awaits and assigns nothing, such as `void 0`. */
function doesNothing(statement: ts.Statement): boolean {
  if (ts.isEmptyStatement(statement)) return true;
  if (!ts.isExpressionStatement(statement)) return false;
  return ![statement.expression, ...descendants(statement.expression)].some(acts);
}

function acts(node: ts.Node): boolean {
  const runs =
    ts.isCallExpression(node) || ts.isNewExpression(node) || ts.isAwaitExpression(node) || ts.isYieldExpression(node)
      || ts.isTaggedTemplateExpression(node);
  return runs || writeTarget(node) !== undefined;
}

function isLogStatement(statement: ts.Statement): boolean {
  if (!ts.isExpressionStatement(statement)) return false;
  const call = awaited(statement.expression);
  return ts.isCallExpression(call) && isLogCall(call);
}
