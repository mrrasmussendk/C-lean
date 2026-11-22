/** Names and symbol IDs: `typescript:<package>:<module path>.<declaration path>(<parameter types>)`. */
import * as ts from "typescript";
import type { FunctionNode } from "../model/project";
import { LANGUAGE } from "../model/ruleTable";
import { ancestors, isFunctionNode, isThisParameter } from "./syntax";

/** The name a function is declared under, or undefined for an anonymous function. */
export function declaredName(node: FunctionNode): string | undefined {
  if (ts.isConstructorDeclaration(node)) return "constructor";
  if (node.name) return node.name.getText();
  const holder = node.parent;
  const isNamedBy = (ts.isVariableDeclaration(holder) || ts.isPropertyDeclaration(holder)) && holder.initializer === node;
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
  if (isFunctionNode(node)) return declaredName(node);
  const isNamedScope = ts.isClassLike(node) || ts.isInterfaceDeclaration(node) || ts.isEnumDeclaration(node) || ts.isModuleDeclaration(node);
  return isNamedScope ? node.name?.getText() : undefined;
}

/** One qualified name per function, anonymous ones numbered in source order within their enclosing symbol. */
export function qualifiedNames(nodes: readonly FunctionNode[]): string[] {
  const scopes = nodes.map(scopeName);
  return nodes.map((node, index) => {
    const own = declaredName(node) ?? lambdaName(nodes, scopes, index);
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

export function typeId(module: Module, qualified: string): string {
  const modulePath = module.path.replace(/\.tsx?$/, "");
  return `${LANGUAGE}:${module.packageName}:${modulePath}.${qualified}`;
}

export function functionId(module: Module, qualified: string, node: FunctionNode): string {
  const types = node.parameters.filter((parameter) => !isThisParameter(parameter)).map(parameterType);
  return `${typeId(module, qualified)}(${types.join(",")})`;
}

/** The declared type as written, without whitespace; `?` when the parameter has no annotation. */
function parameterType(parameter: ts.ParameterDeclaration): string {
  return parameter.type ? parameter.type.getText().replace(/\s+/g, "") : "?";
}
