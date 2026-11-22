// DC03: A numeric literal other than -1, 0 and 1 must be a named constant in production code
import * as ts from "typescript";
import { onNode, productionFiles } from "../analysis/report";
import { descendants, hasModifier } from "../analysis/syntax";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

/** The literal 1 covers -1 as well: the minus is an operator applied to it. */
const ALLOWED_VALUES: ReadonlySet<number> = new Set([0, 1]);

export function DC03(project: Project): Finding[] {
  return productionFiles(project).flatMap((unit) =>
    descendants(unit.source)
      .filter((node) => isNumber(node) && !ALLOWED_VALUES.has(Number(node.text)) && !isNamed(node))
      .map((node) =>
        onNode("DC03", unit.path, node, `numeric literal ${node.getText()} is not a named constant. Declare a constant whose name says what the number means and use the name`),
      ),
  );
}

function isNumber(node: ts.Node): node is ts.NumericLiteral | ts.BigIntLiteral {
  return ts.isNumericLiteral(node) || ts.isBigIntLiteral(node);
}

/** The literal is the whole initializer of a const, of a static readonly member or of an enum member, or it is a type. */
function isNamed(literal: ts.Node): boolean {
  const value = wholeValue(literal);
  const holder = value.parent;
  if (ts.isLiteralTypeNode(holder)) return true;
  if (ts.isEnumMember(holder)) return holder.initializer === value;
  if (ts.isPropertyDeclaration(holder)) return holder.initializer === value && hasModifier(holder, ts.SyntaxKind.StaticKeyword) && hasModifier(holder, ts.SyntaxKind.ReadonlyKeyword);
  return ts.isVariableDeclaration(holder) && holder.initializer === value && (holder.parent.flags & ts.NodeFlags.Const) !== 0;
}

/** The literal with its sign, parentheses and `as const` around it. */
function wholeValue(node: ts.Node): ts.Node {
  const holder = node.parent;
  const wraps = ts.isPrefixUnaryExpression(holder) || ts.isParenthesizedExpression(holder) || ts.isAsExpression(holder) || ts.isSatisfiesExpression(holder);
  return wraps ? wholeValue(holder) : node;
}
