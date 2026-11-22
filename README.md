# C-lean

A merge gate for code written by coding agents. It blocks a change that breaks a rule
in [`spec/spec.md`](spec/spec.md).

One native analyzer package per language writes findings, a dependency graph and
function units to `.gate/out/<language>/`. One core binary, `gate`, reads those files
and the protected files, runs the six contracts, and decides the exit code.

## Layout

| Path | What it is |
| --- | --- |
| `spec/spec.md` | The specification |
| `spec/rules.yaml` | The rule table, machine-readable: ID, definition sentence, default, severity, owner |
| `spec/native-contract.md` | What the spec leaves open, pinned: CLI, outputs, symbol IDs, suppression form, fixture layout |
| `spec/schemas/` | JSON schemas for `graph.json` and `units.json` |
| `core/` | The core, in C#: contracts C1 to C6, the diff-scoped rules, the CLI |
| `packages/<language>/` | The native packages: `csharp`, `typescript`, `python` |
| `corpus/cases/` | Language-neutral conformance cases, one file per rule |
| `corpus/fixtures/<language>/` | One fixture per case per language, with its expected findings |
| `corpus/contract/` | Fixture changes with the expected verdict of the core |
| `harness/` | The corpus harness |
| `tools/selfhost` | Runs every part of the gate through its own language's package |

## Build and check

```sh
dotnet build core -c Release          # the core: core/bin/gate
(cd packages/typescript && npm ci && npm run build)
harness/bin/gate-corpus core          # contract suite, and `gate explain` against spec/rules.yaml
harness/bin/gate-corpus python        # a native package against the corpus (also: csharp, typescript)
                                      # the C# package builds itself on first use
tools/selfhost                        # the gate obeys the gate
```

## Using the gate in a repository

```sh
gate init                             # gate.yaml, a drafted architecture.yaml, CI job, hooks
gate change new add-invoices          # changes/add-invoices.yaml, to fill in
gate change approve add-invoices      # by someone who is not an implementer identity
gate check --base origin/main         # exit 0 pass, 1 blocked, 2 gate failed, 3 conflict filed
gate tests nightly                    # full kill matrix; then: gate tests prune
gate conflict "SZ01 and the task cannot both be satisfied"
```

`gate check` runs each native package on the merge base and on the head, then the
stages `rules`, `delta`, `tests`, `trace` and `judge`. `--stage <name>` runs one.
`--offline` skips git and the native packages and decides on the files already in
`.gate/` (this is how the contract suite runs).

Files a human owns, all protected from the implementer identity:

| File | Content |
| --- | --- |
| `gate.yaml` | Languages, implementer identities, thresholds, protected paths |
| `architecture.yaml` | Components, path globs, allowed edges, effect components, boundaries |
| `changes/<id>.yaml` | One change record per change (C1), with its approval digest |
| `.gate/baseline.yaml` | Fingerprints of accepted existing violations: reported, not enforced |
| `.gate/acks.yaml` | Fingerprints of acknowledged warn-level findings |
| `.gate/mutation-ignore.yaml`, `.gate/mutation-waivers.yaml` | C4 ignore list and human waivers |
| `gate.lock` | Pass verdicts of the judge (C5) |

## Where rules are decided

A rule that can be decided from one snapshot of one language's code lives in the
native package. A rule that needs the base and the head of a change, the change
record, or the merged graph lives in the core: DU01, TS03, FR01, FR03, FR04, HD02,
HD03. `spec/rules.yaml` records the owner of each rule.

## Failures

Every failure says where and what. A finding prints its severity, rule and
`file:line`, then what is wrong with the measured value and the limit and what to do,
then the offending source line. A file the gate cannot use fails with
`<file>:<line>:<column>: <reason>`. Nothing fails with a stack trace or a bare exit code.

## No other tools

The gate assumes nothing is installed besides itself, each language's own toolchain
and the project's own test command. It does not call Stryker, mutmut, a coverage tool,
an OpenTelemetry SDK, ESLint or an indexer.

```yaml
tests:
  commands:        # must write JUnit XML (or TRX) to $GATE_JUNIT; should honour $GATE_TEST_FILES
    python: "gate-python unittest"
    typescript: "node --experimental-strip-types --test --test-reporter=junit --test-reporter-destination=$GATE_JUNIT ${GATE_TEST_FILES:-tests/*.test.ts}"
    csharp: "dotnet test tests/Shop.Tests ${GATE_BUILT:+--no-build} --logger \"trx;LogFileName=$GATE_JUNIT\""
  setup:           # once per fresh checkout
    csharp: "dotnet restore tests/Shop.Tests"
```

With a test command set, `gate check` works in throwaway checkouts of the base and the
head. It runs the suite on both, asks the native package for the mutants of the changed
lines (`gate-<language> mutants`), runs the tests that reach the changed code once per
mutant, then instruments the code (`gate-<language> instrument`) and runs the suite once
more to record which component calls which. `gate tests nightly` does the same for the
whole code and writes the kill matrix that `gate tests prune` reads.

For C#, where every test run would otherwise rebuild, the package compiles all mutants
into the checkout behind a switch (`GATE_MUTANT`). The core then builds once, with
tracing in the same build, and runs one test process per mutant in parallel without
rebuilding; `GATE_BUILT` tells the test command it may skip its build. The base and the
head of a change are processed concurrently. Measured on a 4-core machine: a change with
2 mutants takes about 10 s (30 s before), one with 82 mutants about 48 s, roughly 0.5 s
per mutant. What remains is the start-up of one test host per mutant.

`tools/judge` is the judge command for `gate judge mint`: it calls the Claude Messages
API with the Python standard library only and reads `ANTHROPIC_API_KEY`.

## State

Built and checked:

- The core and its 52 contract fixtures.
- The three native packages, each passing all 190 corpus cases and its own checks of
  `mutants` and `instrument`.
- Self-hosting of the core, the harness and each package, with no suppression and no
  baseline.
- The whole loop on a small repository in each language: suite, mutants, surviving
  mutant reported, traced edge recorded.

Not built, and why:

- A Roslyn `DiagnosticAnalyzer` host, an ESLint plugin and SCIP input. Each is a way
  to plug into another tool; the rules run through the `gate-<language>` commands.
- Checking the gate's own tests under C4. The gate's code is checked by the corpus,
  the contract suite and the package checks, which are not unit tests with a kill matrix.

Not verified: `tools/judge` against the real API (it was run against a local stub).

[`spec/amendments.md`](spec/amendments.md) lists where self-hosting argued against a
rule, and where the three packages read a rule differently.
