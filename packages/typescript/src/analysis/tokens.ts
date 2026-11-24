/** Tokens, code lines and comments of one source file. */
import * as ts from "typescript";
import type { Comment } from "../model/project";
import { lineOf, preorder } from "./syntax";

function isDocumentation(node: ts.Node): boolean {
  return node.kind >= ts.SyntaxKind.FirstJSDocNode && node.kind <= ts.SyntaxKind.LastJSDocNode;
}

/** Every leaf of the full syntax tree, punctuation included, documentation comments excluded. */
export function tokensOf(source: ts.SourceFile): ts.Node[] {
  const parts = (node: ts.Node): readonly ts.Node[] => (isDocumentation(node) ? [] : node.getChildren(source));
  return preorder([source], parts).filter((node) => !isDocumentation(node) && parts(node).length === 0);
}

/** A brace of a JSX expression container that holds no expression: all it can hold is a comment. */
function isEmptyContainer(token: ts.Node): boolean {
  return ts.isJsxExpression(token.parent) && token.parent.expression === undefined;
}

/** Whether the part of a token that lies on one line is more than white space. */
function holdsText(source: ts.SourceFile, token: ts.Node, line: number): boolean {
  const starts = source.getLineStarts();
  const from = Math.max(token.getStart(source), starts[line - 1]);
  const to = Math.min(token.end, starts[line] ?? source.text.length);
  return source.text.slice(from, to).trim() !== "";
}

/** The lines a token holds code on: all it spans, but for JSX text only those that are not blank. */
function tokenLines(source: ts.SourceFile, token: ts.Node): number[] {
  const start = token.getStart(source);
  if (token.end <= start || isEmptyContainer(token)) return [];
  const first = lineOf(source, start);
  const lines = Array.from({ length: lineOf(source, token.end) - first + 1 }, (_, index) => first + index);
  return ts.isJsxText(token) ? lines.filter((line) => holdsText(source, token, line)) : lines;
}

/** The 1-based numbers of the lines that hold code: neither blank nor comment-only. */
export function codeLinesOf(source: ts.SourceFile, tokens: readonly ts.Node[]): ReadonlySet<number> {
  return new Set(tokens.flatMap((token) => tokenLines(source, token)));
}

/** How many code lines lie between two positions, both lines included. */
export function slocBetween(source: ts.SourceFile, codeLines: ReadonlySet<number>, span: ts.TextRange): number {
  const last = lineOf(source, span.end);
  let count = 0;
  for (let line = lineOf(source, span.pos); line <= last; line++) count += Number(codeLines.has(line));
  return count;
}

export function commentsOf(source: ts.SourceFile, tokens: readonly ts.Node[]): Comment[] {
  const ranges = tokens.flatMap((token) => [
    ...(ts.getLeadingCommentRanges(source.text, token.pos) ?? []),
    ...(ts.getTrailingCommentRanges(source.text, token.end) ?? []),
  ]);
  const unique = new Map(ranges.map((range) => [range.pos, range] as const));
  return [...unique.values()].sort((left, right) => left.pos - right.pos).map((range) => commentAt(source, range));
}

function commentAt(source: ts.SourceFile, range: ts.TextRange): Comment {
  const start = source.getLineAndCharacterOfPosition(range.pos);
  const lineStart = source.getPositionOfLineAndCharacter(start.line, 0);
  return {
    text: source.text.slice(range.pos, range.end),
    line: start.line + 1,
    endLine: lineOf(source, range.end),
    ownLine: source.text.slice(lineStart, range.pos).trim() === "",
  };
}
