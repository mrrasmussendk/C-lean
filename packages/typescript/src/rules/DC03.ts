// DC03: A numeric literal other than -1, 0 and 1 must be a named constant in production code
import * as ts from "typescript";
import { onNode, productionFiles } from "../analysis/report";
import { descendants, hasModifier } from "../analysis/syntax";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const ADVICE = "Declare a constant whose name says what the number means and use the name";

/** The literal 1 covers -1 as well: the minus is an operator applied to it. */
const ALLOWED_VALUES: ReadonlySet<number> = new Set([0, 1]);

export function DC03(project: Project): Finding[] {
  return productionFiles(project).flatMap((unit) =>
    descendants(unit.source)
      .filter((node) => isNumber(node) && !ALLOWED_VALUES.has(valueOf(node)) && !isNamed(node))
      .map((node) =>
        onNode("DC03", unit.path, node, `numeric literal ${node.getText()} is not a named constant. ${ADVICE}`),
      ),
  );
}

function isNumber(node: ts.Node): node is ts.NumericLiteral | ts.BigIntLiteral {
  return ts.isNumericLiteral(node) || ts.isBigIntLiteral(node);
}

/** The number a literal stands for; a bigint literal is written with a trailing `n`. */
function valueOf(literal: ts.NumericLiteral | ts.BigIntLiteral): number {
  return Number(literal.text.replace(/n$/, ""));
}

/**
 * The literal is the whole initializer of a const, of a static readonly member or of an enum member, or it is a type,
 * or it is an element of a collection literal in the initializer of a module-level const or a static readonly member.
 */
function isNamed(literal: ts.Node): boolean {
  const value = wholeValue(literal);
  const table = tableOf(value);
  const holder = table.parent;
  if (ts.isLiteralTypeNode(holder)) return true;
  if (ts.isEnumMember(holder)) return holder.initializer === value;
  if (ts.isPropertyDeclaration(
    holder)) return holder.initializer === table && hasModifier(holder, ts.SyntaxKind.StaticKeyword)
      && hasModifier(holder, ts.SyntaxKind.ReadonlyKeyword);
  if (!ts.isVariableDeclaration(holder) || holder.initializer !== table
    || (holder.parent.flags & ts.NodeFlags.Const) === 0) return false;
  return table === value || ts.isSourceFile(holder.parent.parent.parent);
}

/** The literal with its sign, parentheses and `as const` around it. */
function wholeValue(node: ts.Node): ts.Node {
  let value = node;
  while (wraps(value.parent)) value = value.parent;
  return value;
}

function wraps(holder: ts.Node): boolean {
  return ts.isPrefixUnaryExpression(holder) || ts.isParenthesizedExpression(holder) || ts.isAsExpression(holder)
    || ts.isSatisfiesExpression(holder);
}

/** The outermost array or object literal the value is an element of, with what wraps it; else the value itself. */
function tableOf(value: ts.Node): ts.Node {
  let table = value;
  for (let collection = collectionOf(table); collection; collection = collectionOf(table)) {
    table = wholeValue(collection);
  }
  return table;
}

/** The array or object literal that holds the value as an element, as a property's value or as a property's key. */
function collectionOf(value: ts.Node): ts.Node | undefined {
  const holder = value.parent;
  const isMember = ts.isPropertyAssignment(holder) && (holder.initializer === value || holder.name === value);
  const collection = isMember ? holder.parent : holder;
  return ts.isArrayLiteralExpression(collection) || ts.isObjectLiteralExpression(collection) ? collection : undefined;
}
