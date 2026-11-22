// DS01: The same discriminator must not select a branch in more functions than the limit
import * as ts from "typescript";
import { onSymbol, productionFiles } from "../analysis/report";
import { limitOf } from "../analysis/settings";
import { functionFinder, inFileOrder, listed } from "../analysis/spread";
import { bare, descendants } from "../analysis/syntax";
import { declarationsAt, ownDeclarations } from "../analysis/types";
import type { Finding } from "../model/finding";
import type { FunctionUnit, Project } from "../model/project";

const DEFAULT_LIMIT = 2;
const ADVICE = "Give the cases a type of their own or one function that decides";
const COMPARISONS: ReadonlySet<ts.SyntaxKind> = new Set([
  ts.SyntaxKind.EqualsEqualsToken,
  ts.SyntaxKind.ExclamationEqualsToken,
  ts.SyntaxKind.EqualsEqualsEqualsToken,
  ts.SyntaxKind.ExclamationEqualsEqualsToken,
  ts.SyntaxKind.LessThanToken,
  ts.SyntaxKind.LessThanEqualsToken,
  ts.SyntaxKind.GreaterThanToken,
  ts.SyntaxKind.GreaterThanEqualsToken,
]);

/** A use of a discriminator in a branch condition: the declaration that is the discriminator, and where. */
interface Use {
  discriminator: ts.Declaration;
  fn: FunctionUnit;
}

export function DS01(project: Project): Finding[] {
  const limit = limitOf(project.config, "DS01", DEFAULT_LIMIT);
  const holder = functionFinder(project);
  const uses: Use[] = productionFiles(project)
    .flatMap((unit) => descendants(unit.source).flatMap(conditionsOf))
    .flatMap((condition) => {
      const fn = holder(condition);
      return fn ? discriminatorsIn(condition, project.checker).map((discriminator) => ({ discriminator, fn })) : [];
    });
  return [...new Set(uses.map((use) => use.discriminator))]
    .map((discriminator) => ({ discriminator, functions: functionsOf(uses, discriminator) }))
    .filter((measured) => measured.functions.length > limit)
    .map((measured) => {
      const count = `${nameOf(measured.discriminator)} selects a branch in ${measured.functions.length} functions`;
      const wording = `${count} (${listed(measured.functions)}), limit ${limit}. ${ADVICE}`;
      return onSymbol("DS01", measured.functions[0], wording);
    });
}

function functionsOf(uses: readonly Use[], discriminator: ts.Declaration): FunctionUnit[] {
  return inFileOrder([...new Set(uses.filter((use) => use.discriminator === discriminator).map((use) => use.fn))]);
}

function nameOf(discriminator: ts.Declaration): string {
  const name = ts.getNameOfDeclaration(discriminator)?.getText() ?? "(unnamed)";
  if (ts.isEnumDeclaration(discriminator)) return `enum ${name}`;
  return ts.isVariableDeclaration(discriminator) ? `constant ${name}` : `type ${name}`;
}

/** The condition of an if or conditional expression, and the subject and the cases of a switch. */
function conditionsOf(node: ts.Node): ts.Expression[] {
  if (ts.isIfStatement(node) || ts.isSwitchStatement(node) || ts.isCaseClause(node)) return [node.expression];
  return ts.isConditionalExpression(node) ? [node.condition] : [];
}

/** The named constants and enums a condition compares with, and the types it tests for. */
function discriminatorsIn(condition: ts.Expression, checker: ts.TypeChecker): ts.Declaration[] {
  const isSubject = ts.isSwitchStatement(condition.parent) || ts.isCaseClause(condition.parent);
  const compared = [condition, ...descendants(condition)].flatMap((node) => {
    if (!ts.isBinaryExpression(node)) return [];
    if (node.operatorToken.kind === ts.SyntaxKind.InstanceOfKeyword) return testedTypes(node.right, checker);
    const sides = COMPARISONS.has(node.operatorToken.kind) ? [node.left, node.right] : [];
    return sides.flatMap((side) => discriminatorOf(side, checker));
  });
  return [...new Set([...(isSubject ? discriminatorOf(condition, checker) : []), ...compared])];
}

/** The types written under the root that an instanceof names. */
function testedTypes(type: ts.Expression, checker: ts.TypeChecker): ts.Declaration[] {
  return declarationsAt(checker, type).filter((declaration) => !declaration.getSourceFile().isDeclarationFile);
}

/** The module-level constant a value names, or the enum, written under the root, that the value belongs to. */
function discriminatorOf(value: ts.Expression, checker: ts.TypeChecker): ts.Declaration[] {
  const inner = bare(value);
  const constants = ts.isIdentifier(inner) ? declarationsAt(checker, inner).filter(isNamedConstant) : [];
  const enums = ownDeclarations(checker.getTypeAtLocation(inner)).flatMap((declaration) => {
    const owner = ts.isEnumMember(declaration) ? declaration.parent : declaration;
    return ts.isEnumDeclaration(owner) ? [owner] : [];
  });
  return constants.length > 0 ? constants : [...new Set(enums)];
}

function isNamedConstant(declaration: ts.Declaration): boolean {
  if (!ts.isVariableDeclaration(declaration) || (declaration.parent.flags & ts.NodeFlags.Const) === 0) return false;
  const statement = declaration.parent.parent;
  return ts.isVariableStatement(statement) && ts.isSourceFile(statement.parent);
}
