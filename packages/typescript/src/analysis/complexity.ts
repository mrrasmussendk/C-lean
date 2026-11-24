/** Cognitive complexity (SonarSource, G. Ann Campbell, 2017) and nesting depth of a function. */
import * as ts from "typescript";
import type { FunctionNode } from "../model/project";
import { declaredName } from "./naming";
import { bare, childrenOf, isFunctionNode, isLogicalOperator } from "./syntax";

/** Structures that add one plus the current nesting level, and nest what they contain. */
const NESTING_STRUCTURES: ReadonlySet<ts.SyntaxKind> = new Set([
  ts.SyntaxKind.ForStatement,
  ts.SyntaxKind.ForInStatement,
  ts.SyntaxKind.ForOfStatement,
  ts.SyntaxKind.WhileStatement,
  ts.SyntaxKind.DoStatement,
  ts.SyntaxKind.SwitchStatement,
  ts.SyntaxKind.CatchClause,
  ts.SyntaxKind.ConditionalExpression,
]);

/** Control structures counted by nesting depth. A catch or else body is at the depth of its try or if. */
const CONTROL_STRUCTURES: ReadonlySet<ts.SyntaxKind> = new Set([
  ts.SyntaxKind.IfStatement,
  ts.SyntaxKind.ForStatement,
  ts.SyntaxKind.ForInStatement,
  ts.SyntaxKind.ForOfStatement,
  ts.SyntaxKind.WhileStatement,
  ts.SyntaxKind.DoStatement,
  ts.SyntaxKind.SwitchStatement,
  ts.SyntaxKind.TryStatement,
]);

/** One node still to be measured: how deep it is nested, and whether it is the `if` of an else-if. */
interface Visit {
  node: ts.Node;
  nesting: number;
  continuesIf: boolean;
}

export function cognitiveComplexity(fn: FunctionNode, checker: ts.TypeChecker): number {
  const pending: Visit[] = [{ node: fn.body, nesting: 0, continuesIf: false }];
  let total = 0;
  for (let visit = pending.pop(); visit; visit = pending.pop()) {
    total += increment(visit) + Number(isRecursion(visit.node, fn, checker));
    for (const part of partsOf(visit)) pending.push(part);
  }
  return total;
}

/** The children of a node with the nesting they are measured at. */
function partsOf(visit: Visit): Visit[] {
  const node = visit.node;
  if (ts.isIfStatement(node)) return ifParts(node, visit.nesting);
  const nesting = visit.nesting + Number(NESTING_STRUCTURES.has(node.kind) || isFunctionNode(node));
  return childrenOf(node).map((child) => ({ node: child, nesting, continuesIf: false }));
}

/** The condition stays at the level of the if; its branches nest, except an else-if, which continues the if. */
function ifParts(node: ts.IfStatement, nesting: number): Visit[] {
  const inner = nesting + 1;
  const parts =
    [{ node: node.expression, nesting, continuesIf: false }, { node: node.thenStatement, nesting: inner,
      continuesIf: false }];
  const branch = node.elseStatement;
  if (!branch) return parts;
  const continuesIf = ts.isIfStatement(branch);
  return [...parts, { node: branch, nesting: continuesIf ? nesting : inner, continuesIf }];
}

/**
 * An if adds one plus nesting, an else-if one, and a plain else one more; another nesting structure adds one plus
 * nesting; a labelled jump and a run of one boolean operator add one.
 */
function increment(visit: Visit): number {
  const node = visit.node;
  if (ts.isIfStatement(node)) {
    const plainElse = node.elseStatement !== undefined && !ts.isIfStatement(node.elseStatement);
    return (visit.continuesIf ? 1 : 1 + visit.nesting) + Number(plainElse);
  }
  if (NESTING_STRUCTURES.has(node.kind)) return 1 + visit.nesting;
  return Number(isLabelledJump(node) || startsOperatorSequence(node));
}

function isLabelledJump(node: ts.Node): boolean {
  return (ts.isBreakStatement(node) || ts.isContinueStatement(node)) && node.label !== undefined;
}

/** A run of one repeated boolean operator counts once, however long it is. */
function startsOperatorSequence(node: ts.Node): boolean {
  if (!isLogicalOperator(node)) return false;
  const holder = ts.isParenthesizedExpression(node.parent) ? node.parent.parent : node.parent;
  return !(isLogicalOperator(holder) && holder.operatorToken.kind === node.operatorToken.kind);
}

/** A call that resolves to the function itself: by its own name, or as a member of `this`. */
function isRecursion(node: ts.Node, fn: FunctionNode, checker: ts.TypeChecker): boolean {
  const self = declaredName(fn);
  if (!ts.isCallExpression(node) || !self) return false;
  const callee = bare(node.expression);
  const onReceiver = ts.isPropertyAccessExpression(callee) && callee.expression.kind === ts.SyntaxKind.ThisKeyword;
  const name = onReceiver ? callee.name : callee;
  if (!ts.isIdentifier(name) || name.text !== self) return false;
  const declarations = checker.getSymbolAtLocation(name)?.declarations ?? [];
  return declarations.some((declaration) => declaration === fn || declaration === fn.parent);
}

interface Depth {
  node: ts.Node;
  enclosing: number;
}

/** The deepest nesting of control structures at or below a node. A nested function starts again at zero. */
export function nestingDepth(node: ts.Node): number {
  const pending: Depth[] = [{ node, enclosing: 0 }];
  let deepest = 0;
  for (let visit = pending.pop(); visit; visit = pending.pop()) {
    if (isFunctionNode(visit.node)) continue;
    const own = visit.enclosing + Number(CONTROL_STRUCTURES.has(visit.node.kind));
    deepest = Math.max(deepest, own);
    for (const part of depthParts(visit, own)) pending.push(part);
  }
  return deepest;
}

/** An else-if stays at the depth of the if it continues; every other child lies inside the node. */
function depthParts(visit: Depth, own: number): Depth[] {
  return childrenOf(visit.node).map(
    (child) => ({ node: child, enclosing: isElseIf(visit.node, child) ? visit.enclosing : own }));
}

function isElseIf(parent: ts.Node, child: ts.Node): boolean {
  return ts.isIfStatement(parent) && parent.elseStatement === child && ts.isIfStatement(child);
}
