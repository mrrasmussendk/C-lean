// AR01: Flag parameter: a boolean or enum parameter must not be the condition of a branch in the function body
import * as ts from "typescript";
import { onParameter, productionFunctions } from "../analysis/report";
import { bare, descendants, isLogicalOperator } from "../analysis/syntax";
import { unionParts } from "../analysis/types";
import type { Finding } from "../model/finding";
import type { FunctionNode, Project } from "../model/project";

const ADVICE = "Write one function per case and let the caller choose";

const FLAG_TYPES = ts.TypeFlags.BooleanLike | ts.TypeFlags.EnumLike;
const ABSENT_TYPES = ts.TypeFlags.Undefined | ts.TypeFlags.Null;
const EQUALITY: ReadonlySet<ts.SyntaxKind> = new Set([
  ts.SyntaxKind.EqualsEqualsEqualsToken,
  ts.SyntaxKind.ExclamationEqualsEqualsToken,
  ts.SyntaxKind.EqualsEqualsToken,
  ts.SyntaxKind.ExclamationEqualsToken,
]);

type Test = (expression: ts.Expression) => boolean;

export function AR01(project: Project): Finding[] {
  return productionFunctions(project).flatMap((fn) =>
    fn.node.parameters
      .filter((parameter) => isFlag(project.checker, parameter) && branchesOn(project.checker, fn.node, parameter))
      .map((parameter) =>
        onParameter("AR01", fn, parameter,
          `parameter ${parameter.name.getText()} of function ${fn.name} is a flag that selects a branch. ${ADVICE}`),
      ),
  );
}

/** A parameter whose type is boolean or an enum, optionally absent. */
function isFlag(checker: ts.TypeChecker, parameter: ts.ParameterDeclaration): boolean {
  const parts = unionParts(checker.getTypeAtLocation(parameter)).filter((part) => (part.flags & ABSENT_TYPES) === 0);
  return parts.length > 0 && parts.every((part) => (part.flags & FLAG_TYPES) !== 0);
}

function branchesOn(checker: ts.TypeChecker, fn: FunctionNode, parameter: ts.ParameterDeclaration): boolean {
  const isParameter: Test = (expression) =>
    ts.isIdentifier(expression) && checker.getSymbolAtLocation(expression)?.valueDeclaration === parameter;
  return descendants(fn.body).flatMap(conditionOf).some((condition) => tests(condition, isParameter));
}

/** The condition of an if, conditional expression or loop, or the subject of a switch. */
function conditionOf(node: ts.Node): ts.Expression[] {
  if (ts.isIfStatement(node) || ts.isWhileStatement(node) || ts.isDoStatement(node)
    || ts.isSwitchStatement(node)) return [node.expression];
  if (ts.isConditionalExpression(node)) return [node.condition];
  return ts.isForStatement(node) && node.condition ? [node.condition] : [];
}

/** The flag itself, its negation, its comparison with a constant, or a logical and/or containing one of these. */
function tests(condition: ts.Expression, isParameter: Test): boolean {
  const pending = [condition];
  for (let part = pending.pop(); part; part = pending.pop()) {
    const inner = bare(part);
    const below = operandsOf(inner);
    if (below.length === 0 && comparesFlag(inner, isParameter)) return true;
    pending.push(...below);
  }
  return false;
}

/** What a negation or a logical and/or is made of; nothing for any other expression. */
function operandsOf(inner: ts.Expression): ts.Expression[] {
  const isNegation = ts.isPrefixUnaryExpression(inner) && inner.operator === ts.SyntaxKind.ExclamationToken;
  if (isNegation) return [inner.operand];
  return isLogicalOperator(inner) ? [inner.left, inner.right] : [];
}

function comparesFlag(inner: ts.Expression, isParameter: Test): boolean {
  if (ts.isPrefixUnaryExpression(inner)) return false;
  if (!ts.isBinaryExpression(inner)) return isParameter(inner);
  const sides = [bare(inner.left), bare(inner.right)];
  return EQUALITY.has(inner.operatorToken.kind) && sides.some(isParameter) && sides.some(isConstant);
}

function isConstant(expression: ts.Expression): boolean {
  const isLiteral =
    ts.isLiteralExpression(expression) || expression.kind === ts.SyntaxKind.TrueKeyword
      || expression.kind === ts.SyntaxKind.FalseKeyword;
  return isLiteral || expression.kind === ts.SyntaxKind.NullKeyword || ts.isPropertyAccessExpression(expression)
    || expression.getText() === "undefined";
}
