/** Assignments, and whose state they change. */
import * as ts from "typescript";
import { accessRoot, ancestors, bare, hasModifier } from "./syntax";

const STEP_OPERATORS: ReadonlySet<ts.SyntaxKind> =
  new Set([ts.SyntaxKind.PlusPlusToken, ts.SyntaxKind.MinusMinusToken]);

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
  if (root.kind === ts.SyntaxKind.ThisKeyword) return isMemberTarget(target) ? receiverKind(root) : undefined;
  if (!ts.isIdentifier(root)) return undefined;
  const declaration = valueDeclarationOf(root, checker);
  if (!declaration) return undefined;
  if (isModuleVariable(declaration)) return "module";
  return ts.isClassDeclaration(declaration) && isMemberTarget(target) ? "static" : undefined;
}

/** What a name stands for, following an import to the module that declares it. */
function valueDeclarationOf(name: ts.Identifier, checker: ts.TypeChecker): ts.Declaration | undefined {
  const symbol = checker.getSymbolAtLocation(name);
  if (!symbol) return undefined;
  const target = symbol.flags & ts.SymbolFlags.Alias ? checker.getAliasedSymbol(symbol) : symbol;
  return target.valueDeclaration;
}

/** `this` is the class itself in a static member or a static block, and an instance everywhere else. */
function receiverKind(receiver: ts.Node): string {
  const holder = ancestors(receiver).find((ancestor) => !ts.isArrowFunction(ancestor) && bindsReceiver(ancestor));
  const isStatic =
    holder !== undefined
      && (ts.isClassStaticBlockDeclaration(holder) || hasModifier(holder, ts.SyntaxKind.StaticKeyword));
  return isStatic ? "static" : "instance";
}

function bindsReceiver(node: ts.Node): boolean {
  return ts.isFunctionLike(node) || ts.isPropertyDeclaration(node) || ts.isClassStaticBlockDeclaration(node);
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
