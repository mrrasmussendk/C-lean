/** Tokens, code lines and comments of one source file. */
import * as ts from "typescript";
import type { Comment } from "../model/project";
import { lineOf } from "./syntax";

/** Every leaf of the full syntax tree, punctuation included, documentation comments excluded. */
export function tokensOf(source: ts.SourceFile): ts.Node[] {
  const found: ts.Node[] = [];
  const visit = (node: ts.Node): void => {
    if (node.kind >= ts.SyntaxKind.FirstJSDocNode && node.kind <= ts.SyntaxKind.LastJSDocNode) return;
    const children = node.getChildren(source);
    if (children.length === 0) found.push(node);
    children.forEach(visit);
  };
  visit(source);
  return found;
}

/** The 1-based numbers of the lines that hold code: neither blank nor comment-only. */
export function codeLinesOf(source: ts.SourceFile, tokens: readonly ts.Node[]): ReadonlySet<number> {
  const lines = new Set<number>();
  for (const token of tokens) {
    const start = token.getStart(source);
    if (token.end <= start) continue;
    for (let line = lineOf(source, start); line <= lineOf(source, token.end); line++) lines.add(line);
  }
  return lines;
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
