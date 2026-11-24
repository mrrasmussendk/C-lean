/** Builds the function and type units of one source file. */
import * as ts from "typescript";
import type { EffectUse, FunctionNode, FunctionUnit, TypeDeclaration, TypeUnit } from "../model/project";
import { type Module, declaredName, functionIds, nameNode, qualifiedNames, scopeName, typeId } from "./naming";
import { descendants, enclosingFunction, hasModifier, isFunctionNode, startLine } from "./syntax";
import { slocBetween } from "./tokens";

export interface FileFacts extends Module {
  source: ts.SourceFile;
  test: boolean;
  codeLines: ReadonlySet<number>;
  effects: EffectUse[];
}

export function functionUnits(file: FileFacts): FunctionUnit[] {
  const nodes = descendants(file.source).filter(isFunctionNode);
  const names = qualifiedNames(nodes);
  const ids = functionIds(file, names, nodes);
  return nodes.map((node, index) => ({
    node,
    id: ids[index],
    name: names[index],
    path: file.path,
    line: startLine(nameNode(node)),
    anonymous: declaredName(node) === undefined,
    test: file.test,
    sloc: bodySloc(file, node),
    effects: file.effects.filter((use) => enclosingFunction(use.node) === node),
  }));
}

/** Code lines from the first statement of the body to the last; the expression's lines for an arrow expression. */
function bodySloc(file: FileFacts, node: FunctionNode): number {
  const body = node.body;
  if (!ts.isBlock(body)) return slocBetween(file.source, file.codeLines,
    { pos: body.getStart(file.source), end: body.end });
  if (body.statements.length === 0) return 0;
  const first = body.statements[0];
  const last = body.statements[body.statements.length - 1];
  return slocBetween(file.source, file.codeLines, { pos: first.getStart(file.source), end: last.end });
}

function isTypeDeclaration(node: ts.Node): node is TypeDeclaration {
  const isType =
    ts.isClassDeclaration(node) || ts.isInterfaceDeclaration(node) || ts.isEnumDeclaration(node)
      || ts.isTypeAliasDeclaration(node);
  return isType && node.name !== undefined;
}

export function typeUnits(file: FileFacts): TypeUnit[] {
  return descendants(file.source)
    .filter(isTypeDeclaration)
    .map((node) => {
      const scope = scopeName(node);
      const name = node.name?.getText() ?? "";
      const qualified = scope ? `${scope}.${name}` : name;
      return {
        node,
        id: typeId(file, qualified),
        name: qualified,
        kind: isAbstraction(node) ? "abstraction" : "type",
        path: file.path,
        line: startLine(node.name ?? node),
        test: file.test,
        sloc: slocBetween(file.source, file.codeLines, { pos: node.getStart(file.source), end: node.end }),
      };
    });
}

/** An interface or an abstract class. */
function isAbstraction(node: TypeDeclaration): boolean {
  return ts.isInterfaceDeclaration(node) || hasModifier(node, ts.SyntaxKind.AbstractKeyword);
}
