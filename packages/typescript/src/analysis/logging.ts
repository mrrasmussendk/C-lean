/** What counts as a log call, for ER01 and for the logging context of a mutant. */
import type * as ts from "typescript";
import { wordsOf } from "./naming";

/** A call is a log call when its receiver or function name holds one of these as a whole word, without case. */
const LOG_WORDS: ReadonlySet<string> = new Set(["log", "logger", "logging", "console", "print", "trace", "debug"]);

export function isLogCall(call: ts.CallExpression): boolean {
  return wordsOf(call.expression.getText()).some((word) => LOG_WORDS.has(word));
}
