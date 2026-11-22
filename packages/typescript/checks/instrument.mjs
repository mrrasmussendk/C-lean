#!/usr/bin/env node
// Check of `gate-typescript instrument`: the sample project is copied, run, instrumented and run again.
// The output must be unchanged and the trace file must hold exactly the expected component edges.
import { execFileSync } from "node:child_process";
import { cpSync, existsSync, mkdtempSync, readFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const EXPECTED = ['{"from":"app","to":"format"}', '{"from":"app","to":"store"}', '{"from":"store","to":"app"}', '{"from":"store","to":"format"}'];

const here = dirname(fileURLToPath(import.meta.url));
const work = mkdtempSync(join(tmpdir(), "gate-instrument-"));
const copy = join(work, "project");
const traceFile = join(work, "trace.jsonl");
const tool = join(here, "..", "bin", "gate-typescript");
const failures = [];
const expect = (condition, message) => condition || failures.push(message);
const runTests = () => execFileSync(process.execPath, ["--experimental-strip-types", "--no-warnings", join(copy, "run.test.ts")], { encoding: "utf8" });

cpSync(join(here, "samples", "trace"), copy, { recursive: true });
const before = runTests();
const testBefore = readFileSync(join(copy, "run.test.ts"), "utf8");
execFileSync(tool, ["instrument", "--root", copy, "--config", join(copy, "gate.yaml"), "--architecture", join(copy, "architecture.yaml"), "--trace-file", traceFile], { stdio: "inherit" });
const after = runTests();
runTests();

const lines = existsSync(traceFile) ? readFileSync(traceFile, "utf8").split("\n").filter((line) => line !== "") : [];
const edges = [...new Set(lines)].sort();
expect(before === after, `behaviour changed:\n--- before\n${before}--- after\n${after}`);
expect(before.split("\n").length > 8, "the sample did not run to the end");
expect(JSON.stringify(edges) === JSON.stringify(EXPECTED), `trace holds ${JSON.stringify(edges)}, expected ${JSON.stringify(EXPECTED)}`);
expect(lines.length === 2 * EXPECTED.length, `two processes appended ${lines.length} lines, expected each distinct edge once per process (${2 * EXPECTED.length})`);
expect(readFileSync(join(copy, "run.test.ts"), "utf8") === testBefore, "the test file was rewritten");
expect(readFileSync(join(copy, "app/main.ts"), "utf8").includes("__gate.enter("), "app/main.ts was not rewritten");
expect(existsSync(join(copy, "__gate_trace.ts")), "the helper file was not added");

for (const failure of failures) console.log(`FAIL ${failure}`);
console.log(`instrument check: ${edges.length} edges traced, output ${before === after ? "unchanged" : "changed"}, ${failures.length} failures`);
process.exit(failures.length > 0 ? 1 : 0);
