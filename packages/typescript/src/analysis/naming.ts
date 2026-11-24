/** Names and symbol IDs: `typescript:<package>:<module path>.<declaration path>(<parameter types>)`. */
import * as ts from "typescript";
import type { FunctionNode } from "../model/project";
import { LANGUAGE } from "../model/ruleTable";
import { ancestors, hasModifier, isFunctionNode, isThisParameter } from "./syntax";

/** The words of a name in lower case: `showErrorDialog` and `show_error_dialog` are show, error and dialog. */
export function wordsOf(name: string): string[] {
  return (name.match(/[A-Z]+(?![a-z])|[A-Z]?[a-z]+/g) ?? []).map((word) => word.toLowerCase());
}

/** The name a function is declared under, or undefined for an anonymous function. */
export function declaredName(node: FunctionNode): string | undefined {
  if (ts.isConstructorDeclaration(node)) return "constructor";
  if (node.name) return node.name.getText();
  const holder = node.parent;
  const isNamedBy =
    (ts.isVariableDeclaration(holder) || ts.isPropertyDeclaration(holder)) && holder.initializer === node;
  return isNamedBy ? holder.name.getText() : undefined;
}

/** The node whose line a finding on the function is reported on. */
export function nameNode(node: FunctionNode): ts.Node {
  if (ts.isConstructorDeclaration(node)) return node;
  if (node.name) return node.name;
  return declaredName(node) ? (node.parent as ts.NamedDeclaration).name ?? node : node;
}

/** The dotted names of the named declarations around a node, outermost first. */
export function scopeName(node: ts.Node): string {
  return ancestors(node)
    .flatMap((ancestor) => {
      const name = scopeSegment(ancestor);
      return name ? [name] : [];
    })
    .reverse()
    .join(".");
}

function scopeSegment(node: ts.Node): string | undefined {
  if (isFunctionNode(node)) return memberName(node);
  if (ts.isObjectLiteralExpression(node)) return holderName(node);
  const isNamedScope =
    ts.isClassLike(node) || ts.isInterfaceDeclaration(node) || ts.isEnumDeclaration(node)
      || ts.isModuleDeclaration(node);
  return isNamedScope ? node.name?.getText() : undefined;
}

const STATIC_SEGMENT = "static";

/** The declared name, after `static.` for a static member: a class may hold `create` twice, once on each side. */
function memberName(node: FunctionNode): string | undefined {
  const name = declaredName(node);
  const member = ts.isPropertyDeclaration(node.parent) && node.parent.initializer === node ? node.parent : node;
  const isStatic = ts.isClassLike(member.parent) && hasModifier(member, ts.SyntaxKind.StaticKeyword);
  return name !== undefined && isStatic ? `${STATIC_SEGMENT}.${name}` : name;
}

/** The variable or property an object literal is the value of: the scope of the literal's members. */
function holderName(literal: ts.ObjectLiteralExpression): string | undefined {
  let holder: ts.Node = literal.parent;
  while (ts.isParenthesizedExpression(holder) || ts.isAsExpression(holder) || ts.isSatisfiesExpression(holder)) {
    holder = holder.parent;
  }
  const name =
    ts.isVariableDeclaration(holder) || ts.isPropertyAssignment(holder) || ts.isPropertyDeclaration(holder)
      ? holder.name : undefined;
  return name && ts.isIdentifier(name) ? name.text : undefined;
}

/** One qualified name per function, anonymous ones numbered in source order within their enclosing symbol. */
export function qualifiedNames(nodes: readonly FunctionNode[]): string[] {
  const scopes = nodes.map(scopeName);
  return nodes.map((node, index) => {
    const own = memberName(node) ?? lambdaName(nodes, scopes, index);
    return scopes[index] ? `${scopes[index]}.${own}` : own;
  });
}

function lambdaName(nodes: readonly FunctionNode[], scopes: readonly string[], index: number): string {
  const earlier = nodes.slice(0, index).filter((node, at) => scopes[at] === scopes[index] && !declaredName(node));
  return `<lambda>${earlier.length + 1}`;
}

export interface Module {
  path: string;
  packageName: string;
}

/** The extensions of TypeScript sources: `.ts`, `.tsx`, and `.mts` and `.cts` for a fixed module format. */
export const SOURCE_EXTENSION = /\.(?:[cm]?ts|tsx)$/;

export function typeId(module: Module, qualified: string): string {
  const modulePath = module.path.replace(SOURCE_EXTENSION, "");
  return `${LANGUAGE}:${module.packageName}:${modulePath}.${qualified}`;
}

/**
 * The IDs of the functions of one file. Two functions that would share one, such as two declared under one name in
 * different blocks, are told apart by `#2`, `#3` after the name, in source order.
 */
export function functionIds(module: Module, names: readonly string[], nodes: readonly FunctionNode[]): string[] {
  const plain = nodes.map((node, index) => functionId(module, names[index], node));
  const seen = new Map<string, number>();
  return plain.map((id, index) => {
    const count = (seen.get(id) ?? 0) + 1;
    seen.set(id, count);
    return count === 1 ? id : functionId(module, `${names[index]}#${count}`, nodes[index]);
  });
}

function functionId(module: Module, qualified: string, node: FunctionNode): string {
  const types = node.parameters.filter((parameter) => !isThisParameter(parameter)).map(parameterType);
  return `${typeId(module, qualified)}(${types.join(",")})`;
}

/** The declared type as written, without whitespace; `?` when the parameter has no annotation. */
function parameterType(parameter: ts.ParameterDeclaration): string {
  return parameter.type ? parameter.type.getText().replace(/\s+/g, "") : "?";
}
