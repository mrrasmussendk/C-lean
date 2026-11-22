/** Rewrites production sources so that running them records which component calls which. */
import * as path from "node:path";
import * as ts from "typescript";
import type { OutputFile } from "../model/inputs";
import type { FunctionNode, Project, SourceUnit } from "../model/project";
import { productionFiles } from "./report";
import { ancestors, hasModifier, ownDescendants } from "./syntax";
import { HELPER_ALIAS, HELPER_NAME, SCOPE_NAME, helperSource } from "./traceHelper";

/** Text to put at a position. Where several meet, the ones that close come first, innermost first. */
interface Insertion {
  at: number;
  text: string;
  closes: boolean;
  depth: number;
}

export function instrumentedSources(project: Project, traceFile: string): OutputFile[] {
  const units = productionFiles(project);
  const directory = commonDirectory(units.map((unit) => path.posix.dirname(unit.path)));
  const extension = importExtension(project, units);
  const rewritten = units.map((unit) => {
    const relative = path.posix.relative(path.posix.dirname(unit.path), path.posix.join(directory, HELPER_NAME));
    const specifier = `${relative.startsWith(".") ? relative : `./${relative}`}${extension}`;
    return { path: unit.path, text: rewrite(unit, specifier) };
  });
  return [...rewritten, { path: path.posix.join(directory, `${HELPER_NAME}.ts`), text: helperSource(traceFile) }];
}

/** The deepest directory that holds every rewritten file, so the helper stays inside the project's source root. */
function commonDirectory(directories: readonly string[]): string {
  const split = directories.map((directory) => (directory === "." ? [] : directory.split("/")));
  const first = split[0] ?? [];
  const shared = first.filter((_, index) => split.every((parts) => parts.slice(0, index + 1).join("/") === first.slice(0, index + 1).join("/")));
  return shared.join("/");
}

/** The extension the project's own relative imports carry; the compiler options decide when it has none. */
function importExtension(project: Project, units: readonly SourceUnit[]): string {
  const specifiers = units
    .flatMap((unit) => unit.source.statements.filter(ts.isImportDeclaration))
    .flatMap((declaration) => (ts.isStringLiteral(declaration.moduleSpecifier) ? [declaration.moduleSpecifier.text] : []))
    .filter((specifier) => specifier.startsWith("."));
  const written = [".js", ".ts"].find((extension) => specifiers.some((specifier) => specifier.endsWith(extension)));
  return written ?? (specifiers.length > 0 ? "" : project.compiler.importExtension);
}

function rewrite(unit: SourceUnit, specifier: string): string {
  const text = unit.source.text;
  const component = JSON.stringify(unit.component);
  const insertions = unit.functions.flatMap((fn) => insertionsFor(fn.node, component)).sort(inOrder);
  if (insertions.length === 0) return text;
  const head = text.startsWith("#!") ? text.indexOf("\n") + 1 : 0;
  const ends = [...insertions.slice(1).map((insertion) => insertion.at), text.length];
  const pieces = insertions.map((insertion, index) => insertion.text + text.slice(insertion.at, ends[index]));
  const lead = `${text.slice(0, head)}import * as ${HELPER_ALIAS} from ${JSON.stringify(specifier)};\n${text.slice(head, insertions[0].at)}`;
  return lead + pieces.join("");
}

function inOrder(left: Insertion, right: Insertion): number {
  if (left.at !== right.at) return left.at - right.at;
  if (left.closes !== right.closes) return left.closes ? -1 : 1;
  return left.closes ? right.depth - left.depth : left.depth - right.depth;
}

function insertionsFor(fn: FunctionNode, component: string): Insertion[] {
  const depth = ancestors(fn).length;
  const pair = (open: Omit<Insertion, "closes" | "depth">, close: Omit<Insertion, "closes" | "depth">): Insertion[] => [
    { ...open, closes: false, depth },
    { ...close, closes: true, depth },
  ];
  if (fn.asteriskToken) return [...pair(...blockWrap(fn, component)), ...ownDescendants(fn.body).filter(ts.isYieldExpression).flatMap((node) => yieldInsertions(node, fn))];
  if (hasModifier(fn, ts.SyntaxKind.AsyncKeyword)) return pair(...asyncWrap(fn, component));
  return pair(...(ts.isBlock(fn.body) ? blockWrap(fn, component) : expressionWrap(fn.body, component)));
}

type Wrap = [open: { at: number; text: string }, close: { at: number; text: string }];

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
function yieldInsertions(node: ts.YieldExpression, fn: FunctionNode): Insertion[] {
  const depth = ancestors(node).length;
  const operand = node.expression;
  const delegate = hasModifier(fn, ts.SyntaxKind.AsyncKeyword) ? "delegateAsync" : "delegate";
  if (node.asteriskToken && operand) {
    return [
      { at: operand.getStart(), text: `${HELPER_ALIAS}.${delegate}(${SCOPE_NAME}, `, closes: false, depth },
      { at: operand.end, text: ")", closes: true, depth },
    ];
  }
  const leave = `${HELPER_ALIAS}.out(${SCOPE_NAME}, `;
  return [
    { at: node.getStart(), text: `${HELPER_ALIAS}.back(${SCOPE_NAME}, `, closes: false, depth },
    operand ? { at: operand.getStart(), text: leave, closes: false, depth } : { at: node.end, text: ` ${leave}undefined`, closes: true, depth: depth + 1 },
    { at: node.end, text: "))", closes: true, depth },
  ];
}
