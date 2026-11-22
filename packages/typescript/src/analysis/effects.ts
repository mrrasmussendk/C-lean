/** Finds the uses of effect APIs in one source file. */
import * as ts from "typescript";
import type { EffectUse } from "../model/project";
import {
  CLOCK_CONSTRUCTOR,
  EFFECT_CONSTRUCTORS,
  EFFECT_FUNCTIONS,
  EFFECT_IMPORTS,
  EFFECT_MEMBERS,
  EFFECT_MODULES,
  EFFECT_OBJECTS,
  GLOBAL_STATE,
} from "./effectApis";
import { stateKind, writeTarget } from "./state";
import { descendants, dotted, enclosingFunction } from "./syntax";

type Binding = readonly [name: string, group: string];

export function effectUses(source: ts.SourceFile, checker: ts.TypeChecker): EffectUse[] {
  const imported = new Map(source.statements.filter(ts.isImportDeclaration).flatMap(effectBindings));
  return descendants(source).flatMap((node) => [
    ...importUses(node, imported),
    ...memberUses(node),
    ...callUses(node),
    ...globalStateUses(node, checker),
  ]);
}

/** The local names an import declaration binds to effect APIs. */
function effectBindings(declaration: ts.ImportDeclaration): Binding[] {
  const clause = declaration.importClause;
  if (!clause || !ts.isStringLiteral(declaration.moduleSpecifier)) return [];
  const module = declaration.moduleSpecifier.text.replace(/^node:/, "");
  const bindings = clause.namedBindings;
  const whole = [clause.name, bindings && ts.isNamespaceImport(bindings) ? bindings.name : undefined];
  const named = bindings && ts.isNamedImports(bindings) ? [...bindings.elements] : [];
  return [
    ...whole.flatMap((name) => binding(name, EFFECT_MODULES.get(module))),
    ...named.flatMap((element) => binding(element.name, groupOfImport(module, element))),
  ];
}

function groupOfImport(module: string, element: ts.ImportSpecifier): string | undefined {
  const importedName = (element.propertyName ?? element.name).text;
  return EFFECT_MODULES.get(module) ?? EFFECT_IMPORTS.get(`${module}.${importedName}`);
}

function binding(name: ts.Identifier | undefined, group: string | undefined): Binding[] {
  return name && group ? [[name.text, group]] : [];
}

/** A use of an imported effect binding as a receiver or as a callee. */
function importUses(node: ts.Node, imported: ReadonlyMap<string, string>): EffectUse[] {
  if (!ts.isIdentifier(node)) return [];
  const group = imported.get(node.text);
  const user = node.parent;
  const isUse = (ts.isPropertyAccessExpression(user) || ts.isCallExpression(user)) && user.expression === node;
  return group && isUse ? [{ node, group, api: user.getText().split("(")[0] }] : [];
}

function memberUses(node: ts.Node): EffectUse[] {
  if (!ts.isPropertyAccessExpression(node) || !ts.isIdentifier(node.expression)) return [];
  const group = EFFECT_MEMBERS.get(dotted(node)) ?? EFFECT_OBJECTS.get(node.expression.text);
  return group ? [{ node, group, api: dotted(node) }] : [];
}

function callUses(node: ts.Node): EffectUse[] {
  if (!(ts.isCallExpression(node) || ts.isNewExpression(node)) || !ts.isIdentifier(node.expression)) return [];
  const name = node.expression.text;
  const group = ts.isCallExpression(node) ? EFFECT_FUNCTIONS.get(name) : constructorGroup(node, name);
  return group ? [{ node, group, api: name }] : [];
}

function constructorGroup(node: ts.NewExpression, name: string): string | undefined {
  const readsClock = name === CLOCK_CONSTRUCTOR && (node.arguments ?? []).length === 0;
  return readsClock ? "clock" : EFFECT_CONSTRUCTORS.get(name);
}

/** An assignment, inside a function, to a module-level variable or to a static member. */
function globalStateUses(node: ts.Node, checker: ts.TypeChecker): EffectUse[] {
  const target = writeTarget(node);
  if (!target || !enclosingFunction(node)) return [];
  const kind = stateKind(target, checker);
  const isGlobal = kind === "module" || kind === "static";
  return isGlobal ? [{ node, group: GLOBAL_STATE, api: target.getText() }] : [];
}
