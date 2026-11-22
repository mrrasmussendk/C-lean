/** The normalized hash of spec.md: identifiers and literals become placeholders, everything else is kept. */
import * as ts from "typescript";
import { sha256 } from "./digest";
import { childrenOf } from "./syntax";

/** Literal kinds and the placeholder for their type. */
const LITERAL_PLACEHOLDERS: ReadonlyMap<ts.SyntaxKind, string> = new Map([
  [ts.SyntaxKind.NumericLiteral, "#number"],
  [ts.SyntaxKind.BigIntLiteral, "#bigint"],
  [ts.SyntaxKind.StringLiteral, "#string"],
  [ts.SyntaxKind.NoSubstitutionTemplateLiteral, "#string"],
  [ts.SyntaxKind.TemplateHead, "#string"],
  [ts.SyntaxKind.TemplateMiddle, "#string"],
  [ts.SyntaxKind.TemplateTail, "#string"],
  [ts.SyntaxKind.JsxText, "#string"],
  [ts.SyntaxKind.RegularExpressionLiteral, "#regex"],
  [ts.SyntaxKind.TrueKeyword, "#boolean"],
  [ts.SyntaxKind.FalseKeyword, "#boolean"],
  [ts.SyntaxKind.NullKeyword, "#null"],
]);

/** SHA-256 of the fixed-order serialization of the given nodes, taken as one fragment. */
export function normalizedHash(nodes: readonly ts.Node[]): string {
  const placeholders = new Map<string, string>();
  const placeholder = (name: string): string => {
    const known = placeholders.get(name) ?? `$${placeholders.size}`;
    placeholders.set(name, known);
    return known;
  };
  const render = (node: ts.Node): string => {
    if (ts.isIdentifier(node) || ts.isPrivateIdentifier(node)) return placeholder(node.text);
    const literal = LITERAL_PLACEHOLDERS.get(node.kind);
    if (literal) return literal;
    return `(${ts.SyntaxKind[node.kind]}${operatorOf(node)} ${childrenOf(node).map(render).join(" ")})`;
  };
  return sha256(nodes.map(render).join(";"));
}

/** What a node says that is not one of its children: a unary or type operator, and const versus let. */
function operatorOf(node: ts.Node): string {
  const hasOperator = ts.isPrefixUnaryExpression(node) || ts.isPostfixUnaryExpression(node) || ts.isTypeOperatorNode(node);
  if (hasOperator) return `:${ts.SyntaxKind[node.operator]}`;
  return ts.isVariableDeclarationList(node) ? `:${node.flags & ts.NodeFlags.BlockScoped}` : "";
}
