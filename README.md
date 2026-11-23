# C-Lean

A merge gate for code written by coding agents. It blocks a change that breaks a rule
in [`spec/spec.md`](spec/spec.md).

One native analyzer package per language writes findings, a dependency graph and
function units to `.gate/out/<language>/`. One core binary, `gate`, reads those files
and the protected files, runs the six contracts, and decides the exit code.

## Install

One command installs the gate and the package of every language whose toolchain is on
the machine:

```sh
curl -fsSL https://raw.githubusercontent.com/mrrasmussendk/C-lean/main/install.sh | sh
```

or, from a clone (needed while the repository is private): `./install.sh`.

It needs git and the .NET SDK 10 or later. Node 22 and Python 3.12 are needed only for
the TypeScript and Python packages; a missing one is skipped with a note. The gate goes
to `~/.gate`, the commands (`gate`, `gate-csharp`, `gate-typescript`, `gate-python`) to
`~/.local/bin`. Run the same command again to update. `GATE_HOME`, `GATE_BIN` and
`GATE_REF` change the place and the version. Linux and macOS; on Windows use WSL.

### As .NET tools

The core and the C# package are also .NET console tools. From a clone, with the .NET 10
SDK:

```sh
dotnet pack core/src/Gate -c Release -o nupkg
dotnet pack packages/csharp/src/Gate.CSharp -c Release -o nupkg
dotnet tool install --global CLean.Gate --add-source nupkg          # the command: gate
dotnet tool install --global CLean.Gate.CSharp --add-source nupkg   # the command: gate-csharp
```

`gate` finds `gate-csharp` on the PATH. This way installs the C# package only; the
TypeScript and Python packages come with `install.sh`. To update, pack again and run
`dotnet tool update` with the same arguments.

Then, in the repository to be gated:

```sh
gate init
```

`gate init` ends with a checklist of what this repository still needs and the next
step to take; `gate` or `gate help` shows the same checklist under the list of commands:

```
Setup in this repository (3 of 5 done)
  [x] gate.yaml written
  [x] architecture.yaml drafted
  [x] inside a git repository
  [ ] test command set for each language   edit tests.commands in gate.yaml (csharp)
  [ ] first change described               gate change new <id>

Next step: edit tests.commands in gate.yaml (csharp)
```

In a terminal the gate also draws the name C-Lean, colours the checklist, and ends `gate check`
with a picture of a gate standing open or shut. A pipe, a CI log and an agent get plain
lines only, so nothing is spent on decoration there. `NO_COLOR` or `GATE_ART=0` turns it
off in a terminal; `GATE_ART=1` turns it on anywhere.

In GitHub Actions the gate installs itself as an action; `gate init` writes this workflow:

```yaml
- uses: actions/checkout@v4
  with:
    fetch-depth: 0
- uses: mrrasmussendk/C-lean@main
- run: gate check --base "origin/${{ github.base_ref }}"
```

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

## Teaching the agent, and what it costs

The agent should meet the rules before and while it writes, not at merge.

- **Instructions.** `gate init` writes a section into `CLAUDE.md`, generated from
  `gate.yaml` and `architecture.yaml`: how to work, every rule with this repository's
  limit, the components and their allowed edges. `gate instructions` rebuilds it after
  the configuration changes. Only the section between the `gate:begin` and `gate:end`
  markers is replaced.
- **Feedback on every edit.** `gate init` installs an agent hook that runs `gate hook`
  after each edit. It checks the edited file with that language's rules and hands the
  findings straight back to the agent (well under a second on the sample repositories).
  `gate check --file <path>` is the same check by hand.
- **A limit on thrashing.** A finding that is still there after three edits of the file
  comes back with the instruction to stop and run `gate conflict`.
- **Measurement.** Every check appends a line to `.gate/log/checks.jsonl`. `gate stats`
  prints the number and duration of full and per-file checks, which rules stopped the
  most checks, and per branch how many edits were stopped. A rule that costs more
  iterations than it is worth goes back to report level in `gate.yaml`.

The hook gives fast feedback only; if the gate fails inside the hook the edit is not
held up. CI is the gate.

## No other tools

The gate assumes nothing is installed besides itself, each language's own toolchain
and the project's own test command. It does not call Stryker, mutmut, a coverage tool,
an OpenTelemetry SDK, ESLint or an indexer.

```yaml
tests:
  commands:        # must write JUnit XML (or TRX) to $GATE_JUNIT; should honour $GATE_TEST_FILES
    python: "gate-python unittest"
    typescript: "node --experimental-strip-types --test --test-reporter=junit --test-reporter-destination=$GATE_JUNIT ${GATE_TEST_FILES:-tests/*.test.ts}"
    csharp: "dotnet test tests/Shop.Tests${GATE_BUILT:+/bin/Debug/net10.0/Shop.Tests.dll} --logger \"trx;LogFileName=$GATE_JUNIT\""
  setup:           # once per fresh checkout
    csharp: "dotnet restore tests/Shop.Tests"
```

With a test command set, `gate check` works in throwaway checkouts of the base and the
head. It runs the suite on both, asks the native package for the mutants of the changed
lines (`gate-<language> mutants`), runs the tests that reach the changed code once per
mutant, then instruments the code (`gate-<language> instrument`) and runs the suite once
more to record which component calls which. `gate tests nightly` does the same for the
whole code and writes the kill matrix that `gate tests prune` reads.

Each package compiles all mutants into the checkout behind a switch (`GATE_MUTANT`), so
the core builds once, with tracing in the same build, and then runs one test process
per mutant in parallel without rebuilding. Four things keep that fast:

- The suite run reports which mutants any test reaches. A mutant no test reaches is
  recorded as not covered and is not run.
- Each mutant's run gets the tests that reach the mutated function, as files in
  `$GATE_TEST_FILES` and as `|`-separated names in `$GATE_TESTS`.
- `$GATE_BUILT` is set for every run after the first, so a test command can skip its
  build. For .NET, running the built test assembly directly, as above, is about a third
  cheaper per mutant than `dotnet test --no-build`.
- The checkouts of the base and the head are kept between runs (in the temp directory),
  so build outputs are reused, and base and head are processed concurrently.

Measured on a 4-core machine, on the sample repositories used to test the loop:

| Change | Time |
| --- | --- |
| Python, 2 mutants | 1.6 s |
| TypeScript, 2 mutants | 3.3 s |
| C#, 2 mutants | 9 to 10 s (30 s before switching) |
| C#, 82 mutants, 62 of them reached | 32 to 37 s (about 270 s before switching) |

What remains for C# is one cold build and the start-up of one test host per mutant,
about 0.35 s each on four cores.

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
