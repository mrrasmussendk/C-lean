// ER02: Null must not be returned or passed where the type system can express absence. The language's strict null checking is enabled with its warnings as errors
import * as ts from "typescript";
import { sha256 } from "../analysis/digest";
import { onNode, productionFiles } from "../analysis/report";
import { bare, descendants, enclosingFunction } from "../analysis/syntax";
import { someFlag } from "../analysis/types";
import type { Finding } from "../model/finding";
import type { Project } from "../model/project";

const ADVICE = "Declare the absence in the type, or return an empty value or a result object";

const CONFIG_FILE = "tsconfig.json";
const ADMITS_ANYTHING = ts.TypeFlags.Any | ts.TypeFlags.Unknown | ts.TypeFlags.TypeParameter;
const ADMITS_NULL = ADMITS_ANYTHING | ts.TypeFlags.Null;
const ADMITS_UNDEFINED = ADMITS_ANYTHING | ts.TypeFlags.Undefined | ts.TypeFlags.Void;

interface Misuse {
  node: ts.Node;
  problem: string;
}

export function ER02(project: Project): Finding[] {
  const inCode = productionFiles(project).flatMap((unit) =>
    descendants(unit.source)
      .flatMap(
        (node) =>
          [...returned(node, project.checker), ...passed(node, project.checker), ...forced(node, project.checker)])
      .map((misuse) => onNode("ER02", unit.path, misuse.node, `${misuse.problem}. ${ADVICE}`)),
  );
  return [...strictnessFindings(project), ...inCode];
}

/** One finding on line 1 of the project configuration file when it leaves strict null checking off. */
function strictnessFindings(project: Project): Finding[] {
  const facts = project.compiler;
  if (!facts.configPresent || facts.strictNullChecks) return [];
  const message = `ER02: ${CONFIG_FILE} leaves strict null checking off. Set "strict": true in compilerOptions.`;
  return [{ rule: "ER02", file: CONFIG_FILE, line: 1, message, anchor: sha256(facts.configFirstLine.trim()) }];
}

/** The literal null or undefined; empty when the expression is neither. */
function absenceOf(expression: ts.Expression): string {
  const inner = bare(expression);
  if (inner.kind === ts.SyntaxKind.NullKeyword) return "null";
  return ts.isIdentifier(inner) && inner.text === "undefined" ? "undefined" : "";
}

function admits(type: ts.Type, absence: string): boolean {
  return someFlag(type, absence === "null" ? ADMITS_NULL : ADMITS_UNDEFINED);
}

/** A return of null from a function whose declared return type does not admit it. */
function returned(node: ts.Node, checker: ts.TypeChecker): Misuse[] {
  if (!ts.isReturnStatement(node) || !node.expression) return [];
  const absence = absenceOf(node.expression);
  const fn = enclosingFunction(node);
  if (!absence || !fn?.type) return [];
  const declared = checker.getTypeFromTypeNode(fn.type);
  const handed = checker.getAwaitedType(declared) ?? declared;
  return admits(handed, absence) ? []
    : [{ node,
      problem: `${absence} is returned where the declared return type ${fn.type.getText()} does not admit it` }];
}

/** Null passed to a parameter, of a function under the root, whose declared type does not admit it. */
function passed(node: ts.Node, checker: ts.TypeChecker): Misuse[] {
  if (!ts.isCallExpression(node) && !ts.isNewExpression(node)) return [];
  const declaration = checker.getResolvedSignature(node)?.declaration;
  if (!declaration || ts.isJSDocSignature(declaration) || declaration.getSourceFile().isDeclarationFile) return [];
  const parameters = declaration.parameters;
  return (node.arguments ?? []).flatMap((argument, index) => {
    const absence = absenceOf(argument);
    const parameter = parameters[Math.min(index, parameters.length - 1)];
    if (!absence || !parameter?.type || admits(checker.getTypeAtLocation(parameter), absence)) return [];
    const target = `parameter ${parameter.name.getText()}, whose declared type ${parameter.type.getText()}`;
    return [{ node: argument, problem: `${absence} is passed to ${target} does not admit it` }];
  });
}

/** Null forced into a non-null type: `null!`, or a cast chain over null that ends in a type not admitting it. */
function forced(node: ts.Node, checker: ts.TypeChecker): Misuse[] {
  if (ts.isNonNullExpression(node)) {
    const absence = absenceOf(node.expression);
    return absence ? [{ node, problem: `${absence} is forced into a non-null type with !` }] : [];
  }
  if (!isCast(node) || isCast(node.parent)) return [];
  const absence = absenceOf(castOperand(node));
  if (!absence || admits(checker.getTypeFromTypeNode(node.type), absence)) return [];
  return [{ node, problem: `${absence} is cast to ${node.type.getText()}, which does not admit it` }];
}

function isCast(node: ts.Node): node is ts.AsExpression | ts.TypeAssertion {
  return ts.isAsExpression(node) || ts.isTypeAssertionExpression(node);
}

function castOperand(node: ts.Expression): ts.Expression {
  const inner = bare(node);
  return isCast(inner) ? castOperand(inner.expression) : inner;
}
