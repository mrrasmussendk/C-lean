/** Lists the mutants of production code. Nothing is run and no file is changed. */
import * as ts from "typescript";
import type { Project, SourceUnit } from "../model/project";
import { isLogCall } from "./logging";
import { productionFiles } from "./report";
import type { LineRange } from "./settings";
import { ancestors, descendants, hasModifier, isFunctionNode } from "./syntax";
import { someFlag } from "./types";

export interface Mutant {
  id: string;
  file: string;
  line: number;
  column: number;
  mutator: string;
  start: number;
  end: number;
  original: string;
  replacement: string;
  context: string;
  /** `CompileError` on a mutant that was listed but could not be switched in. */
  status?: string;
}

/** A mutant with the token it replaces, for the commands that rewrite sources. */
export interface PlacedMutant {
  mutant: Mutant;
  unit: SourceUnit;
  token: ts.Node;
}

/** One replacement of one token, in character positions of the source text. */
interface Swap {
  mutator: string;
  node: ts.Node;
  start: number;
  end: number;
  replacement: string;
}

type Replacement = readonly [mutator: string, replacement: string];

const BINARY_SWAPS: ReadonlyMap<ts.SyntaxKind, Replacement> = new Map<ts.SyntaxKind, Replacement>([
  [ts.SyntaxKind.PlusToken, ["ArithmeticOperator", "-"]],
  [ts.SyntaxKind.MinusToken, ["ArithmeticOperator", "+"]],
  [ts.SyntaxKind.AsteriskToken, ["ArithmeticOperator", "/"]],
  [ts.SyntaxKind.SlashToken, ["ArithmeticOperator", "*"]],
  [ts.SyntaxKind.EqualsEqualsToken, ["EqualityOperator", "!="]],
  [ts.SyntaxKind.ExclamationEqualsToken, ["EqualityOperator", "=="]],
  [ts.SyntaxKind.EqualsEqualsEqualsToken, ["EqualityOperator", "!=="]],
  [ts.SyntaxKind.ExclamationEqualsEqualsToken, ["EqualityOperator", "==="]],
  [ts.SyntaxKind.LessThanToken, ["EqualityOperator", "<="]],
  [ts.SyntaxKind.LessThanEqualsToken, ["EqualityOperator", "<"]],
  [ts.SyntaxKind.GreaterThanToken, ["EqualityOperator", ">="]],
  [ts.SyntaxKind.GreaterThanEqualsToken, ["EqualityOperator", ">"]],
  [ts.SyntaxKind.AmpersandAmpersandToken, ["LogicalOperator", "||"]],
  [ts.SyntaxKind.BarBarToken, ["LogicalOperator", "&&"]],
]);
const BYTE_ORDER_MARK_BYTES = 3;

export function mutantsOf(project: Project, ranges: readonly LineRange[] | undefined, withMark: ReadonlySet<string>): PlacedMutant[] {
  return productionFiles(project)
    .flatMap((unit) => {
      const offset = withMark.has(unit.path) ? BYTE_ORDER_MARK_BYTES : 0;
      const swaps = descendants(unit.source).filter(isMutable).flatMap((node) => swapsAt(node, project.checker));
      return swaps
        .sort((left, right) => left.start - right.start || compare(left.mutator, right.mutator))
        .map((swap) => ({ mutant: mutantOf(unit, swap, offset), unit, token: swap.node }));
    })
    .filter((placed) => !ranges || ranges.some((range) => isListed(range, placed.mutant)));
}

function isListed(range: LineRange, mutant: Mutant): boolean {
  return range.path === mutant.file && mutant.line >= range.start && mutant.line <= range.end;
}

function compare(left: string, right: string): number {
  if (left === right) return 0;
  return left < right ? -1 : 1;
}

function mutantOf(unit: SourceUnit, swap: Swap, offset: number): Mutant {
  const text = unit.source.text;
  const at = unit.source.getLineAndCharacterOfPosition(swap.start);
  const position = { file: unit.path, line: at.line + 1, column: at.character + 1, mutator: swap.mutator };
  return {
    id: `${position.file}:${position.line}:${position.column}:${position.mutator}`,
    ...position,
    start: offset + Buffer.byteLength(text.slice(0, swap.start), "utf8"),
    end: offset + Buffer.byteLength(text.slice(0, swap.end), "utf8"),
    original: text.slice(swap.start, swap.end),
    replacement: swap.replacement,
    context: contextOf(swap.node),
  };
}

function swapsAt(node: ts.Node, checker: ts.TypeChecker): Swap[] {
  if (ts.isBinaryExpression(node)) return binarySwaps(node, checker);
  const start = node.getStart();
  if (node.kind === ts.SyntaxKind.TrueKeyword) return [{ mutator: "BooleanLiteral", node, start, end: node.end, replacement: "false" }];
  if (node.kind === ts.SyntaxKind.FalseKeyword) return [{ mutator: "BooleanLiteral", node, start, end: node.end, replacement: "true" }];
  const isNegation = ts.isPrefixUnaryExpression(node) && node.operator === ts.SyntaxKind.ExclamationToken;
  if (!isNegation) return [];
  const followsWord = /\w/.test(node.getSourceFile().text.charAt(start - 1));
  return [{ mutator: "UnaryOperator", node, start, end: start + 1, replacement: followsWord ? " " : "" }];
}

function binarySwaps(node: ts.BinaryExpression, checker: ts.TypeChecker): Swap[] {
  const operator = node.operatorToken;
  const swap = BINARY_SWAPS.get(operator.kind);
  if (!swap) return [];
  const start = operator.getStart();
  const isArithmetic = swap[0] === "ArithmeticOperator";
  if (isArithmetic && (joinsStrings(node, checker) || touchesOperator(node.getSourceFile().text, start))) return [];
  return [{ mutator: swap[0], node: operator, start, end: operator.end, replacement: swap[1] }];
}

/** String concatenation, where the types are known to be strings. */
function joinsStrings(node: ts.BinaryExpression, checker: ts.TypeChecker): boolean {
  return [node.left, node.right].some((side) => someFlag(checker.getTypeAtLocation(side), ts.TypeFlags.StringLike));
}

/** `a+-b` must not become `a--b`: an operator written against another sign is left alone. */
function touchesOperator(text: string, start: number): boolean {
  return [text.charAt(start - 1), text.charAt(start + 1)].some((neighbour) => "+-*/".includes(neighbour) && neighbour !== "");
}

/** Not in a decorator, a type annotation, an enum member value or a constant declaration. */
function isMutable(node: ts.Node): boolean {
  const chain = ancestors(node);
  if (chain.some((ancestor) => ts.isDecorator(ancestor) || ts.isTypeNode(ancestor) || ts.isEnumMember(ancestor))) return false;
  const holder = chain.find((ancestor) => isFunctionNode(ancestor) || isConstant(ancestor));
  return holder === undefined || isFunctionNode(holder);
}

/** A module-level `const` or a `static readonly` member; code inside a function below one is not constant. */
function isConstant(node: ts.Node): boolean {
  if (ts.isPropertyDeclaration(node)) return hasModifier(node, ts.SyntaxKind.StaticKeyword) && hasModifier(node, ts.SyntaxKind.ReadonlyKeyword);
  if (!ts.isVariableDeclarationList(node)) return false;
  return (node.flags & ts.NodeFlags.Const) !== 0 && ts.isVariableStatement(node.parent) && ts.isSourceFile(node.parent.parent);
}

/** `logging` inside the arguments of a log call, `guard` inside the condition of an if that only throws. */
function contextOf(node: ts.Node): string {
  const path = [node, ...ancestors(node)].filter((step) => step.parent !== undefined);
  if (path.some((step) => ts.isCallExpression(step.parent) && step.parent.expression !== step && isLogCall(step.parent))) return "logging";
  const guards = path.some((step) => ts.isIfStatement(step.parent) && step.parent.expression === step && onlyThrows(step.parent.thenStatement));
  return guards ? "guard" : "";
}

function onlyThrows(body: ts.Statement): boolean {
  const statements = ts.isBlock(body) ? [...body.statements] : [body];
  return statements.length > 0 && statements.every(ts.isThrowStatement);
}
