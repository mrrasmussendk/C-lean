/** Inline suppressions: the one accepted form, and the tool switches that are not accepted. */
import type { Finding } from "../model/finding";
import type { Comment, Suppression } from "../model/project";
import { CORE_RULES, DEFAULT_SEVERITY } from "../model/ruleTable";
import { contentOf } from "./commentText";

const MARKER = "gate:allow";
export const SUPPRESSION_FORM = `${MARKER} <RULE-ID> reason="<text>" expires=<YYYY-MM-DD> approver=<name>`;

/** Compiler and linter disable switches; a comment with a line that starts with one is a violation. */
const TOOL_SWITCHES: readonly string[] =
  ["eslint-disable", "@ts-ignore", "@ts-expect-error", "@ts-nocheck", "tslint:disable"];

export function toolSwitchOf(comment: Comment): string | undefined {
  const lines = contentOf(comment).split("\n").map((line) => line.trim());
  return TOOL_SWITCHES.find((word) => lines.some((line) => line.startsWith(word)));
}

export function suppressionsOf(comments: readonly Comment[]): Suppression[] {
  return comments.filter((comment) => comment.text.includes(MARKER)).map(suppressionIn);
}

function suppressionIn(comment: Comment): Suppression {
  const before = comment.text.slice(0, comment.text.indexOf(MARKER));
  const body = comment.text.slice(before.length + MARKER.length);
  const line = comment.line + before.split("\n").length - 1;
  return {
    line,
    covers: comment.ownLine ? comment.endLine + 1 : line,
    rule: /^\s+([A-Za-z0-9]+)(?=\s|$|\*)/.exec(body)?.[1] ?? "",
    reason: /\sreason="([^"]+)"/.exec(body)?.[1] ?? "",
    expires: /\sexpires=(\d{4}-\d{2}-\d{2})(?=\s|$|\*)/.exec(body)?.[1] ?? "",
    approver: /\sapprover=([^\s*]+)/.exec(body)?.[1] ?? "",
  };
}

/** What is wrong with the suppression as written, or undefined when it is complete, known and unexpired. */
/** A day that exists: 2027-02-30 and 2027-13-01 have the shape of a date and are none. */
function isDate(text: string): boolean {
  const day = new Date(`${text}T00:00:00Z`);
  return !Number.isNaN(day.getTime()) && day.toISOString().startsWith(text);
}

export function defectOf(suppression: Suppression, today: string): string | undefined {
  if (!suppression.rule) return "names no rule";
  if (!DEFAULT_SEVERITY.has(suppression.rule)) return `names ${suppression.rule}, which is not a rule of the spec`;
  const fields = [["reason", suppression.reason], ["expires", suppression.expires], ["approver", suppression.approver]];
  const missing = fields.filter((field) => !field[1]).map((field) => field[0]);
  if (missing.length > 0) return `of ${suppression.rule} is missing ${missing.join(", ")}`;
  if (!isDate(suppression.expires)) return `of ${suppression.rule} expires on ${suppression.expires}, which is not a day that exists`;
  const expired = suppression.expires < today;
  return expired ? `of ${suppression.rule} expired on ${suppression.expires}, today is ${today}` : undefined;
}

/**
 * A suppression silences findings of its rule on one line: its own when that line also holds code, otherwise the
 * line directly below its comment.
 */
export function silences(suppression: Suppression, finding: Finding): boolean {
  return finding.rule === suppression.rule && finding.line === suppression.covers;
}

/** A valid suppression of a rule this package decides that silences none of the file's findings. */
export function isIdle(suppression: Suppression, findings: readonly Finding[]): boolean {
  return !CORE_RULES.has(suppression.rule) && !findings.some((finding) => silences(suppression, finding));
}
