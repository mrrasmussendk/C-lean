// ER01: A catch block must not be empty and must not only log
import * as ts from "typescript";
import { onNode, productionFiles } from "../analysis/report";
import { awaited, descendants } from "../analysis/syntax";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

/** A call is a log call when its receiver or function name contains one of these, compared without case. */
const LOG_WORDS: readonly string[] = ["log", "logger", "logging", "console", "print", "trace", "debug"];

export function ER01(project: Project): Finding[] {
  return productionFiles(project).flatMap((unit) =>
    descendants(unit.source)
      .filter(ts.isCatchClause)
      .filter((clause) => clause.block.statements.every(isLogCall))
      .map((clause) =>
        onNode("ER01", unit.path, clause, `catch block has ${clause.block.statements.length} statements and none handles the error. Rethrow, wrap, or recover with a stated fallback`),
      ),
  );
}

function isLogCall(statement: ts.Statement): boolean {
  if (!ts.isExpressionStatement(statement)) return false;
  const call = awaited(statement.expression);
  if (!ts.isCallExpression(call)) return false;
  const callee = call.expression.getText().toLowerCase();
  return LOG_WORDS.some((word) => callee.includes(word));
}
