// TS01: A test must contain at least one assertion
import * as ts from "typescript";
import { declaredName } from "../analysis/naming";
import { onSymbol, testFiles } from "../analysis/report";
import { descendants } from "../analysis/syntax";
import { assertNames, assertionsIn, isTestBody } from "../analysis/tests";
import type { Finding } from "../model/finding";
import type { FunctionUnit, Project, SourceUnit } from "../model/project";

export function TS01(project: Project): Finding[] {
  return testFiles(project).flatMap((unit) =>
    unit.functions
      .filter((fn) => isTestBody(fn.node))
      .map((test) => ({ test, assertions: assertionCount(unit, test) }))
      .filter((measured) => measured.assertions < 1)
      .map((measured) =>
        onSymbol("TS01", measured.test, `test ${titleOf(measured.test)} contains ${measured.assertions} assertions, minimum 1. Assert on the behaviour the test name promises`),
      ),
  );
}

/** Assertions in the test itself and in the functions of the same file that it calls directly. */
function assertionCount(unit: SourceUnit, test: FunctionUnit): number {
  const called = new Set(descendants(test.node).filter(ts.isCallExpression).map((call) => call.expression.getText()));
  const helpers = unit.functions.filter((fn) => called.has(declaredName(fn.node) ?? ""));
  const asserts = assertNames(unit.source);
  return [test, ...helpers].flatMap((fn) => assertionsIn(fn.node, asserts)).length;
}

function titleOf(test: FunctionUnit): string {
  const call = test.node.parent;
  const title = ts.isCallExpression(call) ? call.arguments[0] : undefined;
  return title && ts.isStringLiteralLike(title) ? `"${title.text}"` : test.name;
}
