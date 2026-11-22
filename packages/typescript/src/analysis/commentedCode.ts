/** Decides whether comments hold commented-out code. */
import * as ts from "typescript";
import type { Comment } from "../model/project";
import { contentOf, isLineComment } from "./commentText";
import { awaited } from "./syntax";
import { writeTarget } from "./state";

/** Statements that are code whatever they contain. */
const CODE_STATEMENTS: ReadonlySet<ts.SyntaxKind> = new Set([
  ts.SyntaxKind.VariableStatement,
  ts.SyntaxKind.FunctionDeclaration,
  ts.SyntaxKind.ClassDeclaration,
  ts.SyntaxKind.InterfaceDeclaration,
  ts.SyntaxKind.TypeAliasDeclaration,
  ts.SyntaxKind.EnumDeclaration,
  ts.SyntaxKind.ImportDeclaration,
  ts.SyntaxKind.ExportDeclaration,
  ts.SyntaxKind.IfStatement,
  ts.SyntaxKind.ForStatement,
  ts.SyntaxKind.ForInStatement,
  ts.SyntaxKind.ForOfStatement,
  ts.SyntaxKind.WhileStatement,
  ts.SyntaxKind.DoStatement,
  ts.SyntaxKind.SwitchStatement,
  ts.SyntaxKind.TryStatement,
]);

/** The comments that hold code: one per run of consecutive line comments when the run parses as a whole. */
export function commentedCode(comments: readonly Comment[]): Comment[] {
  return runsOf(comments).flatMap((run) => {
    const whole = run.map(contentOf).join("\n");
    if (run.length > 1 && isCode(whole)) return [run[0]];
    return run.filter((comment) => isCode(contentOf(comment)));
  });
}

/** Groups line comments that stand alone on consecutive lines; every other comment is its own run. */
function runsOf(comments: readonly Comment[]): Comment[][] {
  const runs: Comment[][] = [];
  for (const comment of comments) {
    const run = runs[runs.length - 1];
    if (run && continues(run[run.length - 1], comment)) run.push(comment);
    else runs.push([comment]);
  }
  return runs;
}

function continues(previous: Comment, next: Comment): boolean {
  const bothLines = isLineComment(previous) && isLineComment(next) && previous.ownLine && next.ownLine;
  return bothLines && next.line === previous.endLine + 1;
}

/** Text is code when it parses as statements and at least one of them does something. */
export function isCode(text: string): boolean {
  if (!text.trim()) return false;
  const parsed = ts.createSourceFile("comment.ts", text, ts.ScriptTarget.Latest, true);
  return parsed.statements.some((statement) => isCodeStatement(statement, text)) && parsesCleanly(text);
}

function parsesCleanly(text: string): boolean {
  const result = ts.transpileModule(text, { reportDiagnostics: true });
  return (result.diagnostics ?? []).length === 0;
}

function isCodeStatement(statement: ts.Statement, text: string): boolean {
  if (CODE_STATEMENTS.has(statement.kind)) return true;
  if (ts.isLabeledStatement(statement)) return isCodeStatement(statement.statement, text);
  if (ts.isBlock(statement)) return statement.statements.some((inner) => isCodeStatement(inner, text));
  if (ts.isReturnStatement(statement)) return isWrittenReturn(statement, text);
  return ts.isExpressionStatement(statement) && isAction(awaited(statement.expression));
}

/** `return early` is prose; a return of anything but a bare name, or one closed by a semicolon, is code. */
function isWrittenReturn(statement: ts.ReturnStatement, text: string): boolean {
  const returnsName = !statement.expression || ts.isIdentifier(statement.expression);
  return !returnsName || text.includes(";");
}

/** A call, an assignment or a step used as a statement. */
function isAction(expression: ts.Expression): boolean {
  return ts.isCallExpression(expression) || writeTarget(expression) !== undefined;
}
