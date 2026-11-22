/** Assignments, and whose state they change. */
import * as ts from "typescript";
import { accessRoot, bare } from "./syntax";

const STEP_OPERATORS: ReadonlySet<ts.SyntaxKind> = new Set([ts.SyntaxKind.PlusPlusToken, ts.SyntaxKind.MinusMinusToken]);

/** The expression a node writes to: the left side of an assignment, or the operand of ++, -- and delete. */
export function writeTarget(node: ts.Node): ts.Expression | undefined {
  if (ts.isBinaryExpression(node)) return isAssignment(node.operatorToken.kind) ? node.left : undefined;
  if (ts.isPrefixUnaryExpression(node) || ts.isPostfixUnaryExpression(node)) {
    return STEP_OPERATORS.has(node.operator) ? node.operand : undefined;
  }
  return ts.isDeleteExpression(node) ? node.expression : undefined;
}

function isAssignment(operator: ts.SyntaxKind): boolean {
  return operator >= ts.SyntaxKind.FirstAssignment && operator <= ts.SyntaxKind.LastAssignment;
}

/** True when the target is a member or element of an object, not a variable itself. */
export function isMemberTarget(target: ts.Expression): boolean {
  return accessRoot(target) !== bare(target);
}

/** `instance`, `static` or `module` when the written expression is state of that kind, else undefined. */
export function stateKind(target: ts.Expression, checker: ts.TypeChecker): string | undefined {
  const root = accessRoot(target);
  if (root.kind === ts.SyntaxKind.ThisKeyword) return isMemberTarget(target) ? "instance" : undefined;
  if (!ts.isIdentifier(root)) return undefined;
  const declaration = checker.getSymbolAtLocation(root)?.valueDeclaration;
  if (!declaration) return undefined;
  if (isModuleVariable(declaration)) return "module";
  return ts.isClassDeclaration(declaration) && isMemberTarget(target) ? "static" : undefined;
}

function isModuleVariable(declaration: ts.Declaration): boolean {
  if (!ts.isVariableDeclaration(declaration)) return false;
  const statement = declaration.parent.parent;
  return ts.isVariableStatement(statement) && ts.isSourceFile(statement.parent);
}

/** The state-changing writes among the given nodes. */
export function stateWrites(nodes: readonly ts.Node[], checker: ts.TypeChecker): ts.Expression[] {
  return nodes.flatMap((node) => {
    const target = writeTarget(node);
    return target && stateKind(target, checker) ? [target] : [];
  });
}
