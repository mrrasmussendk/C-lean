/** Rewrites production sources so that every listed mutant is compiled in behind a switch. */
import * as ts from "typescript";
import type { OutputFile } from "../model/inputs";
import type { Project, SourceUnit } from "../model/project";
import { type Edit, applyEdits, placeHelper } from "./edits";
import type { Mutant, PlacedMutant } from "./mutants";
import { productionFiles } from "./report";
import { ancestors } from "./syntax";
import { SWITCH_ALIAS, SWITCH_HELPER_NAME, switchHelperSource } from "./switchHelper";

export interface Switching {
  mutants: Mutant[];
  files: OutputFile[];
}

/** The helper function that evaluates each binary operator, by the operator as originally written. */
const BINARY_HELPERS: ReadonlyMap<ts.SyntaxKind, string> = new Map([
  [ts.SyntaxKind.PlusToken, "plus"],
  [ts.SyntaxKind.MinusToken, "minus"],
  [ts.SyntaxKind.AsteriskToken, "times"],
  [ts.SyntaxKind.SlashToken, "divide"],
  [ts.SyntaxKind.EqualsEqualsToken, "eq"],
  [ts.SyntaxKind.ExclamationEqualsToken, "ne"],
  [ts.SyntaxKind.EqualsEqualsEqualsToken, "same"],
  [ts.SyntaxKind.ExclamationEqualsEqualsToken, "differs"],
  [ts.SyntaxKind.LessThanToken, "lt"],
  [ts.SyntaxKind.LessThanEqualsToken, "le"],
  [ts.SyntaxKind.GreaterThanToken, "gt"],
  [ts.SyntaxKind.GreaterThanEqualsToken, "ge"],
]);
const LOGICAL_HELPERS: ReadonlyMap<ts.SyntaxKind, string> = new Map([
  [ts.SyntaxKind.AmpersandAmpersandToken, "and"],
  [ts.SyntaxKind.BarBarToken, "or"],
]);
const NOT_SWITCHED = "CompileError";

/** Type errors a mutant causes (lost narrowing, literal types) must not stop the build: the mutant is under test. */
const UNCHECKED = "// @ts-nocheck\n";

interface Rewritten {
  unit: SourceUnit;
  placed: PlacedMutant[];
  text: string;
}

export function switchedSources(project: Project, placed: readonly PlacedMutant[]): Switching {
  const units = productionFiles(project);
  const helper = placeHelper(project, units, SWITCH_HELPER_NAME);
  const expressible = placed.filter((entry) => editsFor(entry, 0).length > 0);
  const drafts =
    units.map((unit) => draft(unit, expressible, helper.specifierFrom(unit))).filter(
      (entry) => entry.placed.length > 0);
  const sound = drafts.filter((entry) => parses(entry.text));
  const switched = new Set(sound.flatMap((entry) => entry.placed.map((one) => one.mutant.id)));
  return {
    mutants: placed.map(
      (entry) => (switched.has(entry.mutant.id) ? entry.mutant : { ...entry.mutant, status: NOT_SWITCHED })),
    files: [...sound.map((entry) => ({ path: entry.unit.path, text: entry.text })),
      { path: helper.file, text: switchHelperSource(expressible.map((entry) => entry.mutant.id)) }],
  };
}

/** One file with its mutants switched in. A switch names its mutant by its index among all switched mutants. */
function draft(unit: SourceUnit, expressible: readonly PlacedMutant[], specifier: string): Rewritten {
  const placed = expressible.filter((entry) => entry.unit === unit);
  const edits = placed.flatMap((entry) => editsFor(entry, expressible.indexOf(entry)));
  const text = unit.source.text;
  const head = text.startsWith("#!") ? text.indexOf("\n") + 1 : 0;
  const prelude =
    { at: head, remove: 0, text: `${UNCHECKED}import * as ${SWITCH_ALIAS} from ${JSON.stringify(specifier)};\n`,
      closes: false, depth: -1 };
  return { unit, placed, text: applyEdits(text, [prelude, ...edits]) };
}

/**
 * A rewritten file that does not parse is left as it was, and its mutants are reported as not switched. So is one
 * the compiler gives up on: a long chain of operators becomes calls nested deeper than its parser can follow.
 */
function parses(text: string): boolean {
  try {
    return (ts.transpileModule(text, { reportDiagnostics: true }).diagnostics ?? []).length === 0;
  } catch {
    return false;
  }
}

/** The edits that put one mutant behind a switch; none when the mutant cannot be written as an expression switch. */
function editsFor(entry: PlacedMutant, index: number): Edit[] {
  const token = entry.token;
  const call = (name: string): string => `${SWITCH_ALIAS}.${name}(${index}, `;
  const depth = ancestors(token).length;
  if (ts.isBinaryExpression(token.parent)
    && token.parent.operatorToken === token) return binaryEdits(token.parent, call);
  if (ts.isPrefixUnaryExpression(token)) {
    return [
      { at: token.getStart(), remove: 1, text: ` ${call("not")}`, closes: false, depth },
      { at: token.end, remove: 0, text: ")", closes: true, depth },
    ];
  }
  const isBoolean = token.kind === ts.SyntaxKind.TrueKeyword || token.kind === ts.SyntaxKind.FalseKeyword;
  if (!isBoolean) return [];
  return [
    { at: token.getStart(), remove: 0, text: call("bool"), closes: false, depth },
    { at: token.end, remove: 0, text: ")", closes: true, depth },
  ];
}

/**
 * `a + b` becomes `plus(i, a, b)`: both sides are evaluated once, in order, as before.
 * `a && b` becomes `one(and(i, a) ? b : last())`: the right side is still evaluated only when the
 * left side asks for it, and the wrapping call keeps the conditional from joining what stands around it.
 */
function binaryEdits(node: ts.BinaryExpression, call: (name: string) => string): Edit[] {
  const operator = node.operatorToken;
  const depth = ancestors(node).length;
  const width = operator.end - operator.getStart();
  const eager = BINARY_HELPERS.get(operator.kind);
  const lazy = LOGICAL_HELPERS.get(operator.kind);
  if (!eager && !lazy) return [];
  return [
    { at: node.getStart(), remove: 0, text: eager ? call(eager) : `${SWITCH_ALIAS}.one(${call(lazy ?? "")}`,
      closes: false, depth },
    { at: operator.getStart(), remove: width, text: eager ? "," : ") ?", closes: false, depth },
    { at: node.end, remove: 0, text: eager ? ")" : ` : ${SWITCH_ALIAS}.last())`, closes: true, depth },
  ];
}
