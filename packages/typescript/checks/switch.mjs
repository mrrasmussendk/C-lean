#!/usr/bin/env node
// Check of `gate-typescript mutants --switch`: a switched copy of the sample must behave as the original with no
// mutant active, as the byte-wise mutated original with each mutant active, report exactly the mutants it reaches,
// and keep doing so after `instrument` is run on it.
import { execFileSync, spawnSync } from "node:child_process";
import { cpSync, existsSync, mkdtempSync, readFileSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const EXPECTED_EDGES = ['{"from":"app","to":"lib"}', '{"from":"lib","to":"app"}'];

const here = dirname(fileURLToPath(import.meta.url));
const sample = join(here, "samples", "switch");
const work = mkdtempSync(join(tmpdir(), "gate-switch-"));
const tool = join(here, "..", "bin", "gate-typescript");
const failures = [];
const expect = (condition, message) => condition || failures.push(message);
const settings = (root) => ["--config", join(root, "gate.yaml"), "--architecture", join(root, "architecture.yaml")];

function copyOf(name) {
  cpSync(sample, join(work, name), { recursive: true });
  return join(work, name);
}

/** Output and exit status of the project's test command; a mutant may make it fail, never hang. */
function runTests(root, environment) {
  const result = spawnSync(process.execPath, ["--experimental-strip-types", "--no-warnings", join(root, "run.test.ts")], {
    encoding: "utf8",
    timeout: 20000,
    env: { ...process.env, GATE_MUTANT: "", GATE_MUTANT_REACHED: "", ...environment },
  });
  return `${result.stdout}exit ${result.status}`;
}

/** The compiler's complaints about a tree under its own tsconfig.json; empty when it type-checks. */
function typeErrors(root) {
  const result = spawnSync(process.execPath, [join(here, "..", "node_modules", "typescript", "bin", "tsc"), "-p", root], { encoding: "utf8" });
  return result.status === 0 ? "" : result.stdout.trim();
}

const original = runTests(sample, {});
expect(typeErrors(sample) === "", `the sample itself does not type-check:\n${typeErrors(sample)}`);
const switched = copyOf("switched");
execFileSync(tool, ["mutants", "--switch", "--root", switched, "--out", work, ...settings(switched)], { stdio: "inherit" });
const document = JSON.parse(readFileSync(join(work, "mutants.json"), "utf8"));
const mutants = document.mutants;
const live = mutants.filter((mutant) => mutant.status !== "CompileError");

expect(document.switched === true, `mutants.json has "switched": ${document.switched}, expected true`);
expect(mutants.length >= 20, `only ${mutants.length} mutants listed`);
expect(live.length === mutants.length, `${mutants.length - live.length} mutants of the sample were backed out`);
expect(existsSync(join(switched, "__gate_mutant.ts")), "the helper file was not added");
expect(typeErrors(switched) === "", `the switched copy does not type-check:\n${typeErrors(switched)}`);
expect(original.includes("end liftoff") && original.endsWith("exit 0"), "the sample did not run to the end");
expect(runTests(switched, {}) === original, "with no mutant active the switched copy does not behave as the original");
expect(runTests(switched, { GATE_MUTANT: "no/such.ts:1:1:None" }) === original, "an unknown mutant id changes behaviour");

// The same mutant list without --switch describes the original bytes.
execFileSync(tool, ["mutants", "--root", sample, "--out", join(work, "plain"), ...settings(sample)], { stdio: "inherit" });
const plain = JSON.parse(readFileSync(join(work, "plain", "mutants.json"), "utf8")).mutants;
expect(JSON.stringify(plain) === JSON.stringify(mutants), "start, end and replacement no longer describe the original file");

let changed = 0;
const bytewise = new Map();
for (const mutant of live) {
  const mutated = copyOf(`mutated-${live.indexOf(mutant)}`);
  const bytes = readFileSync(join(mutated, mutant.file));
  writeFileSync(join(mutated, mutant.file), Buffer.concat([bytes.subarray(0, mutant.start), Buffer.from(mutant.replacement, "utf8"), bytes.subarray(mutant.end)]));
  const expected = runTests(mutated, {});
  const actual = runTests(switched, { GATE_MUTANT: mutant.id });
  bytewise.set(mutant.id, expected);
  changed += Number(expected !== original);
  expect(actual === expected, `${mutant.id}: switched on it gives\n${actual}\nbut the byte-wise mutated original gives\n${expected}`);
}
expect(changed >= 10, `only ${changed} mutants change the output; the sample does not exercise the switches`);

// Reached: every mutant except those in the function nobody calls.
const source = readFileSync(join(sample, "lib/calc.ts"), "utf8").split("\n");
const deadStart = source.findIndex((line) => line.includes("function neverCalled")) + 1;
const deadEnd = source.findIndex((line, index) => index >= deadStart && line === "}") + 1;
const isDead = (mutant) => mutant.file === "lib/calc.ts" && mutant.line >= deadStart && mutant.line <= deadEnd;
const reachedFile = join(work, "reached.txt");
runTests(switched, { GATE_MUTANT_REACHED: reachedFile });
runTests(switched, { GATE_MUTANT_REACHED: reachedFile });
const reachedLines = readFileSync(reachedFile, "utf8").split("\n").filter((line) => line !== "");
const reached = [...new Set(reachedLines)].sort();
const expectedReached = live.filter((mutant) => !isDead(mutant)).map((mutant) => mutant.id).sort();
expect(mutants.some(isDead), "the sample has no mutant in the uncalled function");
expect(JSON.stringify(reached) === JSON.stringify(expectedReached), `reached ${JSON.stringify(reached)}\nexpected ${JSON.stringify(expectedReached)}`);
expect(reachedLines.length === 2 * reached.length, `two processes wrote ${reachedLines.length} lines, expected each reached id once per process`);

// instrument on top of the switched copy: traces right, switches still working.
const traceFile = join(work, "trace.jsonl");
execFileSync(tool, ["instrument", "--root", switched, ...settings(switched), "--trace-file", traceFile], { stdio: "inherit" });
expect(typeErrors(switched) === "", `the switched and instrumented copy does not type-check:\n${typeErrors(switched)}`);
expect(runTests(switched, {}) === original, "after instrument the switched copy does not behave as the original");
const edges = [...new Set(readFileSync(traceFile, "utf8").split("\n").filter((line) => line !== ""))].sort();
expect(JSON.stringify(edges) === JSON.stringify(EXPECTED_EDGES), `trace holds ${JSON.stringify(edges)}, expected ${JSON.stringify(EXPECTED_EDGES)}`);
expect(!readFileSync(join(switched, "__gate_mutant.ts"), "utf8").includes("__gate.enter("), "instrument rewrote the mutant helper");
expect(readFileSync(join(switched, "lib/calc.ts"), "utf8").includes("__gate.enter(") && readFileSync(join(switched, "lib/calc.ts"), "utf8").includes("__gateMutant."), "lib/calc.ts is not both switched and instrumented");
let stillSwitching = 0;
for (const mutant of live) {
  const actual = runTests(switched, { GATE_MUTANT: mutant.id });
  stillSwitching += Number(actual === bytewise.get(mutant.id));
  expect(actual === bytewise.get(mutant.id), `${mutant.id}: after instrument the switch no longer matches the byte-wise mutant`);
}

for (const failure of failures) console.log(`FAIL ${failure}`);
console.log(`switch check: ${live.length} of ${mutants.length} mutants switched, ${changed} change the output, ${reached.length} reached, ${stillSwitching} still match after instrument, ${edges.length} edges traced, type-checks, ${failures.length} failures`);
process.exit(failures.length > 0 ? 1 : 0);
