/** The text of a comment without its comment markers. */
import type { Comment } from "../model/project";

export function isLineComment(comment: Comment): boolean {
  return comment.text.startsWith("//");
}

function isDocComment(comment: Comment): boolean {
  return comment.text.startsWith("/**");
}

/** Lines of the comment with `//`, the block delimiters and leading `*` removed. */
function contentLines(comment: Comment): string[] {
  if (isLineComment(comment)) return [comment.text.replace(/^\/+/, "")];
  const inside = comment.text.replace(/^\/\*+/, "").replace(/\*+\/$/, "");
  return inside.split("\n").map((line) => line.replace(/^\s*\*/, ""));
}

/** A documentation comment's example section runs from an `@example` tag to the next tag. */
function withoutExamples(lines: readonly string[]): string[] {
  const tagAt = (index: number): string => lines[index].trim().split(/\s/)[0];
  const lastTag = (index: number): string =>
    lines
      .slice(0, index + 1)
      .map((_, at) => tagAt(at))
      .filter((word) => word.startsWith("@"))
      .pop() ?? "";
  return lines.filter((_, index) => lastTag(index) !== "@example");
}

/** What the comment says, as plain text. Example sections of documentation comments are left out. */
export function contentOf(comment: Comment): string {
  const lines = contentLines(comment);
  return (isDocComment(comment) ? withoutExamples(lines) : lines).join("\n").trim();
}
