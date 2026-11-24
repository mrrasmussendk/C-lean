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
import { awaited, descendants, dotted, enclosingFunction } from "./syntax";

type Binding = readonly [name: string, group: string];

export function effectUses(source: ts.SourceFile, checker: ts.TypeChecker): EffectUse[] {
  const nodes = descendants(source);
  const imported =
    new Map([...source.statements.filter(ts.isImportDeclaration).flatMap(effectBindings),
      ...nodes.flatMap(requiredBindings)]);
  return nodes.flatMap((node) => [
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
    ...named.flatMap(
      (element) => binding(element.name, groupOfImport(module, (element.propertyName ?? element.name).text))),
  ];
}

function groupOfImport(module: string, importedName: string): string | undefined {
  return EFFECT_MODULES.get(module) ?? EFFECT_IMPORTS.get(`${module}.${importedName}`);
}

/** The local names bound by `import x = require("m")`, `const x = require("m")` and `const x = await import("m")`. */
function requiredBindings(node: ts.Node): Binding[] {
  if (ts.isImportEqualsDeclaration(node)) {
    const reference = node.moduleReference;
    const module = ts.isExternalModuleReference(reference) ? moduleName(reference.expression) : undefined;
    return module === undefined ? [] : binding(node.name, EFFECT_MODULES.get(module));
  }
  if (!ts.isVariableDeclaration(node) || !node.initializer) return [];
  const module = loadedModule(awaited(node.initializer));
  if (module === undefined) return [];
  if (ts.isIdentifier(node.name)) return binding(node.name, EFFECT_MODULES.get(module));
  return ts.isObjectBindingPattern(node.name) ? node.name.elements.flatMap((element) => picked(module, element)) : [];
}

/** The module a `require("m")` or `import("m")` call loads. */
function loadedModule(call: ts.Expression): string | undefined {
  if (!ts.isCallExpression(call)) return undefined;
  const callee = call.expression;
  const loads = callee.kind === ts.SyntaxKind.ImportKeyword || (ts.isIdentifier(callee) && callee.text === "require");
  return loads && call.arguments.length > 0 ? moduleName(call.arguments[0]) : undefined;
}

function moduleName(specifier: ts.Expression): string | undefined {
  return ts.isStringLiteralLike(specifier) ? specifier.text.replace(/^node:/, "") : undefined;
}

/** `{ readFileSync }` or `{ readFileSync: read }` taken out of a loaded module. */
function picked(module: string, element: ts.BindingElement): Binding[] {
  const importedName = element.propertyName ?? element.name;
  if (!ts.isIdentifier(importedName) || !ts.isIdentifier(element.name)) return [];
  return binding(element.name, groupOfImport(module, importedName.text));
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
