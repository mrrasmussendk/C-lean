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

type Naming = (name: string) => string;
type Piece = ts.Node | string;

/** SHA-256 of the fixed-order serialization of the given nodes, taken as one fragment. */
export function normalizedHash(nodes: readonly ts.Node[]): string {
  const placeholders = new Map<string, string>();
  const placeholder: Naming = (name) => {
    const known = placeholders.get(name) ?? `$${placeholders.size}`;
    placeholders.set(name, known);
    return known;
  };
  return sha256(nodes.map((node) => rendered(node, placeholder)).join(";"));
}

/** `(Kind child child)` for a node, a placeholder for an identifier or a literal. No recursion: trees can be deep. */
function rendered(root: ts.Node, placeholder: Naming): string {
  const written: string[] = [];
  const pending: Piece[] = [root];
  for (let piece = pending.pop(); piece !== undefined; piece = pending.pop()) {
    const leaf = typeof piece === "string" ? piece : leafOf(piece, placeholder);
    if (leaf !== undefined) written.push(leaf);
    else for (const part of opened(piece as ts.Node)) pending.push(part);
  }
  return written.join("");
}

function leafOf(node: ts.Node, placeholder: Naming): string | undefined {
  if (ts.isIdentifier(node) || ts.isPrivateIdentifier(node)) return placeholder(node.text);
  return LITERAL_PLACEHOLDERS.get(node.kind);
}

/** The pieces of an inner node in the order they are taken from the end of the work list: last piece first. */
function opened(node: ts.Node): Piece[] {
  const children = childrenOf(node).flatMap((child, index): Piece[] => (index === 0 ? [child] : [" ", child]));
  return [`(${ts.SyntaxKind[node.kind]}${operatorOf(node)} `, ...children, ")"].reverse();
}

/** What a node says that is not one of its children: a unary or type operator, and const versus let. */
function operatorOf(node: ts.Node): string {
  const hasOperator =
    ts.isPrefixUnaryExpression(node) || ts.isPostfixUnaryExpression(node) || ts.isTypeOperatorNode(node);
  if (hasOperator) return `:${ts.SyntaxKind[node.operator]}`;
  return ts.isVariableDeclarationList(node) ? `:${node.flags & ts.NodeFlags.BlockScoped}` : "";
}
