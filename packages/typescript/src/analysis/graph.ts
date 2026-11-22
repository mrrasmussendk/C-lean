/** Extracts the dependency graph: nodes for functions and types, edges resolved by the type checker. */
import * as ts from "typescript";
import type { Project, SourceUnit } from "../model/project";
import { ancestors, descendants } from "./syntax";
import { declarationsAt } from "./types";
import { isPublic } from "./visibility";

export interface GraphNode {
  id: string;
  kind: string;
  component: string;
  file: string;
  line: number;
  test: boolean;
  public: boolean;
  effect: boolean;
}

export interface GraphEdge {
  from: string;
  to: string;
  kind: string;
}

export interface Graph {
  nodes: GraphNode[];
  edges: GraphEdge[];
}

interface Index {
  checker: ts.TypeChecker;
  functions: ReadonlyMap<ts.Node, string>;
  types: ReadonlyMap<ts.Node, string>;
}

interface Reference {
  kind: string;
  targets: readonly ts.Node[];
}

export function graphOf(project: Project): Graph {
  const index: Index = {
    checker: project.checker,
    functions: new Map(project.files.flatMap((unit) => unit.functions.map((fn) => [fn.node, fn.id] as const))),
    types: new Map(project.files.flatMap((unit) => unit.types.map((type) => [type.node, type.id] as const))),
  };
  const edges = project.files.flatMap((unit) => descendants(unit.source).flatMap((node) => edgesAt(node, index)));
  const distinct = new Map(edges.filter((edge) => edge.from !== edge.to).map((edge) => [`${edge.from} ${edge.to} ${edge.kind}`, edge]));
  return {
    nodes: project.files.flatMap(nodesOf).sort((left, right) => compare(left.id, right.id)),
    edges: [...distinct.keys()].sort(compare).flatMap((key) => distinct.get(key) ?? []),
  };
}

function compare(left: string, right: string): number {
  if (left === right) return 0;
  return left < right ? -1 : 1;
}

function nodesOf(unit: SourceUnit): GraphNode[] {
  const shared = { component: unit.component, file: unit.path, test: unit.test };
  return [
    ...unit.types.map((type) => ({ ...shared, id: type.id, kind: type.kind, line: type.line, public: isPublic(type.node), effect: false })),
    ...unit.functions.map((fn) => ({ ...shared, id: fn.id, kind: "function", line: fn.line, public: isPublic(fn.node), effect: fn.effects.length > 0 })),
  ];
}

/** The edges a node gives rise to, from the nearest enclosing function or type. */
function edgesAt(node: ts.Node, index: Index): GraphEdge[] {
  const reference = callAt(node, index) ?? referenceAt(node, index);
  if (!reference) return [];
  const from = ancestors(node).flatMap((ancestor) => idOf(ancestor, index))[0];
  const to = reference.targets.flatMap((target) => idOf(target, index))[0];
  return from && to ? [{ from, to, kind: reference.kind }] : [];
}

function idOf(node: ts.Node, index: Index): string[] {
  const id = index.functions.get(node) ?? index.types.get(node);
  return id ? [id] : [];
}

/** A call targets the called function, or the abstraction that declares the called member without a body. */
function callAt(node: ts.Node, index: Index): Reference | undefined {
  if (!ts.isCallExpression(node)) return undefined;
  const declaration = index.checker.getResolvedSignature(node)?.declaration;
  return declaration ? { kind: "call", targets: [declaration, declaration.parent] } : undefined;
}

/** construct, inherit and type_reference edges target the types a name resolves to. */
function referenceAt(node: ts.Node, index: Index): Reference | undefined {
  const named = nameAt(node);
  if (!named) return undefined;
  const targets = declarationsAt(index.checker, named.name).filter((declaration) => index.types.has(declaration));
  return { kind: named.kind, targets };
}

interface Named {
  kind: string;
  name: ts.Node;
}

function nameAt(node: ts.Node): Named | undefined {
  if (ts.isNewExpression(node)) return { kind: "construct", name: node.expression };
  if (ts.isExpressionWithTypeArguments(node)) return { kind: "inherit", name: node.expression };
  if (ts.isTypeReferenceNode(node)) return { kind: "type_reference", name: node.typeName };
  if (ts.isTypeQueryNode(node)) return { kind: "type_reference", name: node.exprName };
  return ts.isIdentifier(node) && isValueUse(node) ? { kind: "type_reference", name: node } : undefined;
}

/** A name used as a value, such as an enum in `Colour.Red`, and not already covered by another edge kind. */
function isValueUse(node: ts.Identifier): boolean {
  const user = node.parent;
  if (ts.isNewExpression(user) || ts.isExpressionWithTypeArguments(user)) return false;
  if (ts.isTypeReferenceNode(user) || ts.isTypeQueryNode(user) || ts.isQualifiedName(user)) return false;
  return !(ts.isPropertyAccessExpression(user) && user.name === node);
}
