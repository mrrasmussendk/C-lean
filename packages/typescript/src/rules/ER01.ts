// ER01: A catch block must not be empty and must not only log
import * as ts from "typescript";
import { isLogCall } from "../analysis/logging";
import { onNode, productionFiles } from "../analysis/report";
import { awaited, descendants } from "../analysis/syntax";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

export function ER01(project: Project): Finding[] {
  return productionFiles(project).flatMap((unit) =>
    descendants(unit.source)
      .filter(ts.isCatchClause)
      .filter((clause) => clause.block.statements.every(isLogStatement))
      .map((clause) =>
        onNode("ER01", unit.path, clause, `catch block has ${clause.block.statements.length} statements and none handles the error. Rethrow, wrap, or recover with a stated fallback`),
      ),
  );
}

function isLogStatement(statement: ts.Statement): boolean {
  if (!ts.isExpressionStatement(statement)) return false;
  const call = awaited(statement.expression);
  return ts.isCallExpression(call) && isLogCall(call);
}
