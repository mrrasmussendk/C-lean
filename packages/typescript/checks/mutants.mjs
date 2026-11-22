#!/usr/bin/env node
// Check of `gate-typescript mutants`: every mutant of the sample, applied to the file's bytes, must change
// the file and leave it parsing; the mutated token must be what the mutant says; exclusions must hold.
import { execFileSync } from "node:child_process";
import { mkdtempSync, readFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import ts from "typescript";

const here = dirname(fileURLToPath(import.meta.url));
const sample = join(here, "samples", "mutants");
const out = mkdtempSync(join(tmpdir(), "gate-mutants-"));
const tool = join(here, "..", "bin", "gate-typescript");
const failures = [];
const expect = (condition, message) => condition || failures.push(message);

function run(extra) {
  execFileSync(tool, ["mutants", "--root", sample, "--out", out, "--config", join(sample, "gate.yaml"), "--architecture", join(sample, "architecture.yaml"), ...extra], { stdio: "inherit" });
  const document = JSON.parse(readFileSync(join(out, "mutants.json"), "utf8"));
  expect(document.switched === false, `mutants.json has "switched": ${document.switched} with ${JSON.stringify(extra)}, expected false`);
  return document.mutants;
}

const mutants = run([]);
for (const mutant of mutants) {
  const bytes = readFileSync(join(sample, mutant.file));
  const mutated = Buffer.concat([bytes.subarray(0, mutant.start), Buffer.from(mutant.replacement, "utf8"), bytes.subarray(mutant.end)]);
  const lineStart = bytes.subarray(0, mutant.start).lastIndexOf("\n") + 1;
  expect(bytes.subarray(mutant.start, mutant.end).toString("utf8") === mutant.original, `${mutant.id}: bytes ${mutant.start}..${mutant.end} are not "${mutant.original}"`);
  expect(!mutated.equals(bytes), `${mutant.id}: the mutated file does not differ`);
  expect(bytes.subarray(0, mutant.start).toString("utf8").split("\n").length === mutant.line, `${mutant.id}: line does not locate start`);
  expect(bytes.subarray(lineStart, mutant.start).toString("utf8").length + 1 === mutant.column, `${mutant.id}: column does not locate start`);
  const diagnostics = ts.transpileModule(mutated.toString("utf8"), { reportDiagnostics: true, fileName: mutant.file }).diagnostics ?? [];
  expect(diagnostics.length === 0, `${mutant.id}: the mutated file does not parse: ${diagnostics.map((d) => ts.flattenDiagnosticMessageText(d.messageText, " ")).join("; ")}`);
}

const ids = mutants.map((mutant) => mutant.id);
const sorted = [...mutants].sort((a, b) => a.file.localeCompare(b.file) || a.start - b.start || a.mutator.localeCompare(b.mutator));
const lineOf = (needle) => readFileSync(join(sample, "src/pricing.ts"), "utf8").split("\n").findIndex((line) => line.includes(needle)) + 1;
const onLine = (needle) => mutants.filter((mutant) => mutant.line === lineOf(needle));
expect(new Set(ids).size === ids.length, "mutant ids are not unique");
expect(JSON.stringify(sorted.map((m) => m.id)) === JSON.stringify(ids), "mutants are not sorted by file, start, mutator");
expect(mutants.every((mutant) => mutant.file === "src/pricing.ts"), "a mutant is in test code");
for (const needle of ["const RATE", "const ENABLED", "Low = 1 + 1", "static readonly START", "const greeting", "greeting + ", "Array<true | false>", "amount+-quantity"]) {
  const allowed = needle === "Array<true | false>" ? 2 : 0;
  expect(onLine(needle).length === allowed, `line "${needle}" has ${onLine(needle).length} mutants, expected ${allowed}`);
}
expect(onLine("logger.warn").length === 2 && onLine("logger.warn").every((mutant) => mutant.context === "logging"), "mutants in the log call are not marked logging");
expect(onLine("if (!ENABLED").length === 3 && onLine("if (!ENABLED").every((mutant) => mutant.context === "guard"), "mutants in the throwing guard are not marked guard");
expect(onLine("amount * quantity").map((m) => m.replacement).join("") === "/+*", "arithmetic operators are not swapped as specified");
expect(onLine("typeof!flags").some((mutant) => mutant.mutator === "UnaryOperator" && mutant.replacement === " "), "negation after a word is not replaced by a space");
const mutators = new Set(mutants.map((mutant) => mutant.mutator));
for (const mutator of ["ArithmeticOperator", "EqualityOperator", "LogicalOperator", "BooleanLiteral", "UnaryOperator"]) expect(mutators.has(mutator), `no ${mutator} mutant`);

const linesFile = join(out, "lines.yaml");
const { writeFileSync } = await import("node:fs");
writeFileSync(linesFile, `lines:\n  - {path: src/pricing.ts, start: ${lineOf("next(value")}, end: ${lineOf("return value !==")}}\n`);
const selected = run(["--lines", linesFile]);
expect(selected.length === 3 && selected.every((mutant) => mutant.line === lineOf("return value !==")), `--lines selected ${selected.length} mutants, expected the 3 on the listed line`);

for (const failure of failures) console.log(`FAIL ${failure}`);
console.log(`mutants check: ${mutants.length} mutants applied byte-wise, switched false without --switch, ${failures.length} failures`);
process.exit(failures.length > 0 ? 1 : 0);
