/** Public or not: private, #private and non-exported top-level declarations are non-public. */
import * as ts from "typescript";
import { hasModifier } from "./syntax";

/** The declaration that carries the modifiers: the variable statement for a variable, else the node itself. */
function holderOf(node: ts.Node): ts.Node {
  const isInitializer = ts.isArrowFunction(node) || ts.isFunctionExpression(node);
  const declaration =
    isInitializer && (ts.isVariableDeclaration(node.parent) || ts.isPropertyDeclaration(node.parent)) ? node.parent
      : node;
  return ts.isVariableDeclaration(declaration) ? declaration.parent.parent : declaration;
}

export function isPrivateMember(member: ts.Node): boolean {
  const name = (member as ts.NamedDeclaration).name;
  return hasModifier(member, ts.SyntaxKind.PrivateKeyword) || (name !== undefined && ts.isPrivateIdentifier(name));
}

/** A class member that is not private, or a top-level declaration that is exported. */
export function isPublic(node: ts.Node): boolean {
  const holder = holderOf(node);
  if (ts.isClassLike(holder.parent)) return !isPrivateMember(holder);
  return ts.isSourceFile(holder.parent) && hasModifier(holder, ts.SyntaxKind.ExportKeyword);
}
