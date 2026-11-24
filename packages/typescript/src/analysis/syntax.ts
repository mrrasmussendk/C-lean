/** Small, pure helpers over the TypeScript syntax tree. */
import * as ts from "typescript";
import type { FunctionNode } from "../model/project";

const FUNCTION_KINDS: ReadonlySet<ts.SyntaxKind> = new Set([
  ts.SyntaxKind.FunctionDeclaration,
  ts.SyntaxKind.MethodDeclaration,
  ts.SyntaxKind.Constructor,
  ts.SyntaxKind.GetAccessor,
  ts.SyntaxKind.SetAccessor,
  ts.SyntaxKind.FunctionExpression,
  ts.SyntaxKind.ArrowFunction,
]);

export function isFunctionNode(node: ts.Node): node is FunctionNode {
  return FUNCTION_KINDS.has(node.kind) && (node as Partial<FunctionNode>).body !== undefined;
}

export function childrenOf(node: ts.Node): ts.Node[] {
  const children: ts.Node[] = [];
  ts.forEachChild(node, (child) => {
    children.push(child);
  });
  return children;
}

/** The given nodes and every node `expand` yields below them, in source order. No recursion: trees can be deep. */
export function preorder(roots: readonly ts.Node[], expand: (node: ts.Node) => readonly ts.Node[]): ts.Node[] {
  const found: ts.Node[] = [];
  const pending = [...roots].reverse();
  for (let current = pending.pop(); current; current = pending.pop()) {
    found.push(current);
    const below = expand(current);
    for (let index = below.length - 1; index >= 0; index--) pending.push(below[index]);
  }
  return found;
}

/** Every node below the given one, in source order. */
export function descendants(node: ts.Node): ts.Node[] {
  return preorder(childrenOf(node), childrenOf);
}

/** Every node below the given one that is not inside a nested function. The nested function itself is included. */
export function ownDescendants(node: ts.Node): ts.Node[] {
  return preorder(childrenOf(node), (child) => (isFunctionNode(child) ? [] : childrenOf(child)));
}

/** The parents of a node, nearest first. */
export function ancestors(node: ts.Node): ts.Node[] {
  const chain: ts.Node[] = [];
  for (let current: ts.Node | undefined = node.parent; current; current = current.parent) chain.push(current);
  return chain;
}

export function enclosingFunction(node: ts.Node): FunctionNode | undefined {
  return ancestors(node).find(isFunctionNode);
}

export function lineOf(source: ts.SourceFile, position: number): number {
  return source.getLineAndCharacterOfPosition(position).line + 1;
}

export function startLine(node: ts.Node): number {
  const source = node.getSourceFile();
  return lineOf(source, node.getStart(source));
}

export function endLine(node: ts.Node): number {
  return lineOf(node.getSourceFile(), node.end);
}

/** The expression without enclosing parentheses. */
export function bare(expression: ts.Expression): ts.Expression {
  let inner = expression;
  while (ts.isParenthesizedExpression(inner)) inner = inner.expression;
  return inner;
}

/** The expression without parentheses and await. */
export function awaited(expression: ts.Expression): ts.Expression {
  let inner = bare(expression);
  while (ts.isAwaitExpression(inner)) inner = bare(inner.expression);
  return inner;
}

type Access = ts.PropertyAccessExpression | ts.ElementAccessExpression | ts.NonNullExpression;

function isAccess(node: ts.Expression): node is Access {
  return ts.isPropertyAccessExpression(node) || ts.isElementAccessExpression(node) || ts.isNonNullExpression(node);
}

/** The object an access path starts from: `a` for `a.b[0].c`. */
export function accessRoot(expression: ts.Expression): ts.Expression {
  let inner = bare(expression);
  while (isAccess(inner)) inner = bare(inner.expression);
  return inner;
}

/** The identifier a call chain starts from: `vi` for `vi.fn().mockReturnValue(5)`; empty when there is none. */
export function calleeRoot(expression: ts.Expression): string {
  let root = accessRoot(expression);
  while (ts.isCallExpression(root) || ts.isTaggedTemplateExpression(root)) {
    root = accessRoot(ts.isCallExpression(root) ? root.expression : root.tag);
  }
  return ts.isIdentifier(root) ? root.text : "";
}

/** `a.b` for a property access on a plain identifier, else empty. */
export function dotted(node: ts.Node): string {
  const simple = ts.isPropertyAccessExpression(node) && ts.isIdentifier(node.expression);
  return simple ? `${node.expression.text}.${node.name.text}` : "";
}

export function hasModifier(node: ts.Node, kind: ts.SyntaxKind): boolean {
  const modifiers = ts.canHaveModifiers(node) ? ts.getModifiers(node) ?? [] : [];
  return modifiers.some((modifier) => modifier.kind === kind);
}

export function isThisParameter(parameter: ts.ParameterDeclaration): boolean {
  return ts.isIdentifier(parameter.name) && parameter.name.text === "this";
}

export function sum(values: readonly number[]): number {
  return values.reduce((total, value) => total + value, 0);
}

export function isLogicalOperator(node: ts.Node): node is ts.BinaryExpression {
  if (!ts.isBinaryExpression(node)) return false;
  const operator = node.operatorToken.kind;
  return operator === ts.SyntaxKind.AmpersandAmpersandToken || operator === ts.SyntaxKind.BarBarToken;
}
