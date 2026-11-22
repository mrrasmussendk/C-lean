// TS02: A test must not assert only on values it configured a test double to return
import * as ts from "typescript";
import { onSymbol, testFiles } from "../analysis/report";
import { bare, descendants } from "../analysis/syntax";
import { assertNames, assertionsIn, doubleNames, isCallOnDouble, isTestBody } from "../analysis/tests";
import type { Finding } from "../model/finding";
import type { FunctionNode, Project, SourceUnit } from "../model/project";

export function TS02(project: Project): Finding[] {
  return testFiles(project).flatMap((unit) =>
    unit.functions
      .filter((fn) => isTestBody(fn.node))
      .map((test) => ({ test, subjects: assertedValues(unit, test.node) }))
      .filter((measured) => measured.subjects.length > 0 && measured.subjects.every((subject) => subject.fromDouble))
      .map((measured) =>
        onSymbol("TS02", measured.test, `test ${measured.test.name} makes ${measured.subjects.length} assertions and all are on values returned by its own test doubles. Assert on what the code under test returns or does`),
      ),
  );
}

interface Subject {
  fromDouble: boolean;
}

/** One subject per assertion: whether its actual value is a call on a double, or a variable assigned from one. */
function assertedValues(unit: SourceUnit, test: FunctionNode): Subject[] {
  const doubles = doubleNames(unit.source);
  const assigned = descendants(test)
    .filter(ts.isVariableDeclaration)
    .filter((declaration) => declaration.initializer && isCallOnDouble(declaration.initializer, doubles))
    .map((declaration) => declaration.name.getText());
  const values = new Set(assigned);
  return assertionsIn(test, assertNames(unit.source)).map((assertion) => {
    const actual = assertion.arguments[0] ? bare(assertion.arguments[0]) : undefined;
    const isValue = actual !== undefined && ts.isIdentifier(actual) && values.has(actual.text);
    return { fromDouble: isValue || (actual !== undefined && isCallOnDouble(actual, doubles)) };
  });
}
