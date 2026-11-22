/** Cognitive complexity (SonarSource, G. Ann Campbell, 2017) and nesting depth of a function. */
import * as ts from "typescript";
import type { FunctionNode } from "../model/project";
import { declaredName } from "./naming";
import { bare, childrenOf, isFunctionNode, isLogicalOperator, sum } from "./syntax";

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

interface Level {
  nesting: number;
  self: string;
}

export function cognitiveComplexity(fn: FunctionNode): number {
  return cost(fn.body, { nesting: 0, self: declaredName(fn) ?? "" });
}

function cost(node: ts.Node, level: Level): number {
  if (ts.isIfStatement(node)) return ifCost(node, level, 1 + level.nesting);
  const nests = NESTING_STRUCTURES.has(node.kind) || isFunctionNode(node);
  const inner = { ...level, nesting: level.nesting + Number(nests) };
  return increment(node, level) + sum(childrenOf(node).map((child) => cost(child, inner)));
}

/** An if adds one plus nesting; an else-if and an else add one each without a nesting increment. */
function ifCost(node: ts.IfStatement, level: Level, own: number): number {
  const inner = { ...level, nesting: level.nesting + 1 };
  const branch = node.elseStatement;
  const condition = cost(node.expression, level) + cost(node.thenStatement, inner);
  if (!branch) return own + condition;
  return own + condition + (ts.isIfStatement(branch) ? ifCost(branch, level, 1) : 1 + cost(branch, inner));
}

function increment(node: ts.Node, level: Level): number {
  if (NESTING_STRUCTURES.has(node.kind)) return 1 + level.nesting;
  return Number(isLabelledJump(node) || startsOperatorSequence(node) || isRecursion(node, level.self));
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

function isRecursion(node: ts.Node, self: string): boolean {
  if (!ts.isCallExpression(node) || !self) return false;
  const callee = bare(node.expression);
  if (ts.isIdentifier(callee)) return callee.text === self;
  const onReceiver = ts.isPropertyAccessExpression(callee) && callee.expression.kind === ts.SyntaxKind.ThisKeyword;
  return onReceiver && callee.name.text === self;
}

/** The deepest nesting of control structures at or below a node. A nested function starts again at zero. */
export function nestingDepth(node: ts.Node): number {
  if (isFunctionNode(node)) return 0;
  const children = childrenOf(node);
  const chained = deepest(children.filter((child) => isElseIf(node, child)));
  const inner = deepest(children.filter((child) => !isElseIf(node, child)));
  return Math.max(chained, inner + Number(CONTROL_STRUCTURES.has(node.kind)));
}

function deepest(nodes: readonly ts.Node[]): number {
  return Math.max(0, ...nodes.map(nestingDepth));
}

/** An else-if stays at the depth of the if it continues. */
function isElseIf(parent: ts.Node, child: ts.Node): boolean {
  return ts.isIfStatement(parent) && parent.elseStatement === child && ts.isIfStatement(child);
}
