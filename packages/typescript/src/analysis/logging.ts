/** What counts as a log call, for ER01 and for the logging context of a mutant. */
import type * as ts from "typescript";

/** A call is a log call when its receiver or function name contains one of these, compared without case. */
const LOG_WORDS: readonly string[] = ["log", "logger", "logging", "console", "print", "trace", "debug"];

export function isLogCall(call: ts.CallExpression): boolean {
  const callee = call.expression.getText().toLowerCase();
  return LOG_WORDS.some((word) => callee.includes(word));
}
