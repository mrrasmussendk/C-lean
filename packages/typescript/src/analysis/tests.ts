/** Recognises tests, assertions and test doubles of Jest, Vitest and Mocha by name. */
import * as ts from "typescript";
import type { FunctionNode } from "../model/project";
import { awaited, calleeRoot, descendants, dotted } from "./syntax";

const TEST_FUNCTIONS: ReadonlySet<string> = new Set(["it", "test", "specify"]);
const ASSERT_MODULES: ReadonlySet<string> = new Set(["assert", "assert/strict", "node:assert", "node:assert/strict"]);
const DOUBLE_FRAMEWORKS: ReadonlySet<string> = new Set(["jest", "vi"]);
const EXPECT = "expect";

/** A function passed to it(...), test(...) or one of their variants such as it.each(...)(...). */
export function isTestBody(node: FunctionNode): boolean {
  const call = node.parent;
  return ts.isCallExpression(call) && call.arguments.some((argument) => argument === node)
    && TEST_FUNCTIONS.has(calleeRoot(call.expression));
}

/** The local names that reach node:assert in this file: `assert` itself and whatever is imported from it. */
export function assertNames(source: ts.SourceFile): ReadonlySet<string> {
  const imports = source.statements
    .filter(ts.isImportDeclaration)
    .filter((declaration) =>
      ts.isStringLiteral(declaration.moduleSpecifier) && ASSERT_MODULES.has(declaration.moduleSpecifier.text));
  const names =
    imports.flatMap((declaration) => descendants(declaration).filter(ts.isIdentifier).map((name) => name.text));
  return new Set(["assert", ...names]);
}

/** The assertions below a node: expect(...) calls, and calls of assert or of a function imported from it. */
export function assertionsIn(node: ts.Node, asserts: ReadonlySet<string>): ts.CallExpression[] {
  return descendants(node).filter(ts.isCallExpression).filter((call) => isAssertion(call, asserts));
}

function isAssertion(call: ts.CallExpression, asserts: ReadonlySet<string>): boolean {
  const callee = call.expression;
  if (ts.isIdentifier(callee)) return callee.text === EXPECT || asserts.has(callee.text);
  return ts.isPropertyAccessExpression(callee) && ts.isIdentifier(callee.expression)
    && asserts.has(callee.expression.text);
}

/** The variables of the file that hold a framework double: jest.fn(), vi.fn(), or an object literal of them. */
export function doubleNames(source: ts.SourceFile): ReadonlySet<string> {
  const declarations = descendants(source).filter(ts.isVariableDeclaration);
  const doubles = declarations.filter((declaration) => declaration.initializer && makesDouble(declaration.initializer));
  return new Set(doubles.map((declaration) => declaration.name.getText()));
}

function makesDouble(initializer: ts.Expression): boolean {
  if (DOUBLE_FRAMEWORKS.has(calleeRoot(initializer))) return true;
  if (!ts.isObjectLiteralExpression(initializer)) return false;
  return initializer.properties.some(
    (property) => ts.isPropertyAssignment(property) && DOUBLE_FRAMEWORKS.has(calleeRoot(property.initializer)));
}

/** Whether the expression is a call on one of the doubles. */
export function isCallOnDouble(expression: ts.Expression, doubles: ReadonlySet<string>): boolean {
  const value = awaited(expression);
  return ts.isCallExpression(value) && doubles.has(calleeRoot(value.expression));
}

const PROPERTY_CALLS: ReadonlySet<string> = new Set(["fc.assert", "fc.property"]);

/** A property-based test with fast-check: test.prop / it.prop, or a body that calls fc.assert or fc.property. */
export function isPropertyTest(node: FunctionNode): boolean {
  if (!isTestBody(node)) return false;
  const declaredWithProp =
    descendants(node.parent).some(
      (part) =>
        ts.isPropertyAccessExpression(part) && part.name.text === "prop" && TEST_FUNCTIONS.has(calleeRoot(part)));
  return declaredWithProp
    || descendants(node).some((part) => ts.isCallExpression(part) && PROPERTY_CALLS.has(dotted(part.expression)));
}
