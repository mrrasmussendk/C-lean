/** Rewrites production sources so that running them records which component calls which. */
import * as ts from "typescript";
import type { OutputFile } from "../model/inputs";
import type { FunctionNode, Project, SourceUnit } from "../model/project";
import { type Edit, applyEdits, placeHelper } from "./edits";
import { productionFiles } from "./report";
import { ancestors, hasModifier, ownDescendants } from "./syntax";
import { HELPER_ALIAS, HELPER_NAME, SCOPE_NAME, helperSource } from "./traceHelper";

export function instrumentedSources(project: Project, traceFile: string): OutputFile[] {
  const units = productionFiles(project);
  const helper = placeHelper(project, units, HELPER_NAME);
  const rewritten = units.map((unit) => ({ path: unit.path, text: rewrite(unit, helper.specifierFrom(unit)) }));
  return [...rewritten, { path: helper.file, text: helperSource(traceFile) }];
}

/** The import goes before the first statement, so that leading comments such as a shebang or a pragma stay first. */
function rewrite(unit: SourceUnit, specifier: string): string {
  const component = JSON.stringify(unit.component);
  const edits = unit.functions.flatMap((fn) => insertionsFor(fn.node, component));
  const first = unit.source.statements[0];
  if (edits.length === 0 || !first) return unit.source.text;
  const helperImport =
    { at: first.getStart(), remove: 0, text: `import * as ${HELPER_ALIAS} from ${JSON.stringify(specifier)};\n`,
      closes: false, depth: -1 };
  return applyEdits(unit.source.text, [helperImport, ...edits]);
}

function insertionsFor(fn: FunctionNode, component: string): Edit[] {
  const depth = ancestors(fn).length;
  const pair = (open: Spot, close: Spot): Edit[] => [
    { ...open, remove: 0, closes: false, depth },
    { ...close, remove: 0, closes: true, depth },
  ];
  if (fn.asteriskToken) return [...pair(...blockWrap(fn, component)),
    ...ownDescendants(fn.body).filter(ts.isYieldExpression).flatMap((node) => yieldInsertions(node, fn))];
  if (hasModifier(fn, ts.SyntaxKind.AsyncKeyword)) return pair(...asyncWrap(fn, component));
  return pair(...(ts.isBlock(fn.body) ? blockWrap(fn, component) : expressionWrap(fn.body, component)));
}

type Spot = Pick<Edit, "at" | "text">;
type Wrap = [open: Spot, close: Spot];

/** Enter on the way in, exit in a finally: the previous component is restored on return and on throw. */
function blockWrap(fn: FunctionNode, component: string): Wrap {
  const body = fn.body as ts.Block;
  const lead = body.statements.filter((statement, index) => index === leadLength(body, index) && isLead(statement));
  const at = lead.length > 0 ? lead[lead.length - 1].end : body.getStart() + 1;
  return [
    { at, text: ` const ${SCOPE_NAME} = ${HELPER_ALIAS}.enter(${component}); try {` },
    { at: body.end - 1, text: ` } finally { ${HELPER_ALIAS}.exit(${SCOPE_NAME}); } ` },
  ];
}

/** The index itself while every statement before it is a lead statement; used to keep only the unbroken prefix. */
function leadLength(body: ts.Block, index: number): number {
  return body.statements.slice(0, index).every(isLead) ? index : -1;
}

/** A directive such as "use strict", or the super(...) call that must stay first in a constructor. */
function isLead(statement: ts.Statement): boolean {
  if (!ts.isExpressionStatement(statement)) return false;
  const expression = statement.expression;
  const isSuperCall = ts.isCallExpression(expression) && expression.expression.kind === ts.SyntaxKind.SuperKeyword;
  return ts.isStringLiteral(expression) || isSuperCall;
}

function expressionWrap(body: ts.Expression, component: string): Wrap {
  return [
    { at: body.getStart(), text: `{ const ${SCOPE_NAME} = ${HELPER_ALIAS}.enter(${component}); try { return ` },
    { at: body.end, text: `; } finally { ${HELPER_ALIAS}.exit(${SCOPE_NAME}); } }` },
  ];
}

/** An async function runs its body inside the component's context, which then follows it across every await. */
function asyncWrap(fn: FunctionNode, component: string): Wrap {
  const returns = fn.type ? `: ${fn.type.getText()}` : "";
  const call = `${HELPER_ALIAS}.run(${component}, async ()${returns} => `;
  if (!ts.isBlock(fn.body)) return [{ at: fn.body.getStart(), text: call }, { at: fn.body.end, text: ")" }];
  return [
    { at: fn.body.getStart() + 1, text: ` return ${call}{` },
    { at: fn.body.end - 1, text: " }); " },
  ];
}

/** A generator leaves its component while it is suspended at a yield and takes it up again when resumed. */
function yieldInsertions(node: ts.YieldExpression, fn: FunctionNode): Edit[] {
  const depth = ancestors(node).length;
  const operand = node.expression;
  const delegate = hasModifier(fn, ts.SyntaxKind.AsyncKeyword) ? "delegateAsync" : "delegate";
  if (node.asteriskToken && operand) {
    return [
      { at: operand.getStart(), text: `${HELPER_ALIAS}.${delegate}(${SCOPE_NAME}, `, remove: 0, closes: false, depth },
      { at: operand.end, text: ")", remove: 0, closes: true, depth },
    ];
  }
  const leave = `${HELPER_ALIAS}.out(${SCOPE_NAME}, `;
  return [
    { at: node.getStart(), text: `${HELPER_ALIAS}.back(${SCOPE_NAME}, `, remove: 0, closes: false, depth },
    operand ? { at: operand.getStart(), text: leave, remove: 0, closes: false, depth }
      : { at: node.end, text: ` ${leave}undefined`, remove: 0, closes: true, depth: depth + 1 },
    { at: node.end, text: "))", remove: 0, closes: true, depth },
  ];
}
