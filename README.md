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
harness/bin/gate-corpus core          # contract suite, and `gate explain` against spec/rules.yaml
harness/bin/gate-corpus python        # a native package against the corpus (also: csharp, typescript)
tools/selfhost                        # the gate obeys the gate
```

## Using the gate in a repository

```sh
gate init                             # gate.yaml, a drafted architecture.yaml, CI job, hooks
gate change new add-invoices          # changes/add-invoices.yaml, to fill in
gate change approve add-invoices      # by someone who is not an implementer identity
gate check --base origin/main         # exit 0 pass, 1 blocked, 2 gate failed, 3 conflict filed
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
