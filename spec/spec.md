# Clean code gate specification

Nov 22, 2026 · @Marc Rasmussen

## Scope and principles

The gate blocks a merge when a change breaks a rule in this spec. It is built as one native analyzer package per language plus one shared core binary, and it is aimed at code written by coding agents.

**In scope**

- Deterministic code rules (size, arguments, side effects, duplication, error handling, coupling, dead code, fragmentation).
- Five contracts: architecture delta, trace check, test admission, verdict lockfile, hardening.
- An LLM judge for the questions no deterministic rule can answer. It is advisory until measured.

**Out of scope**

- Formatting and style.
- Formal verification.
- Rewriting rules that a mature linter already implements correctly. A native package may wrap an existing linter if the result passes the conformance corpus.

**Principles**

1. **Deterministic first.** A rule that can be decided from syntax or the semantic model is never given to the judge.
2. **One definition per rule.** Every rule has an ID, a language-neutral definition, a default threshold and a severity. Native packages implement the definition and add nothing.
3. **Signals constrain each other.** Size limits are checked against clone detection and fragmentation signals, declared edges against observed edges, test existence against mutant kills. Satisfying one by gaming it trips another.
4. **The implementer cannot edit the gate.** Config, baselines, the architecture model, ignore lists and the lockfile are read-only to whoever writes the change.
5. **There is a sanctioned exit.** An implementer that cannot satisfy the gate files a conflict and stops. It does not work around the rule.
6. **The gate obeys the gate.** Its own code passes every rule at blocking severity (see Self-hosting).

**Severity levels**

| Level | Effect |
| --- | --- |
| block | Fails the check; the change cannot merge |
| warn | Shown on the change; needs a human acknowledgement to merge |
| report | Recorded as a trend; never stops a change |

A new rule ships at report, and is promoted only after it has been measured on real changes.

**Assumptions made in this draft**

- Languages for the first release: C#, TypeScript and Python.
- Working name: `gate` for the CLI, `gate.yaml` for the config. No product name chosen.
- All thresholds are starting values and are configurable. The Basis column in the rule spec says which rest on evidence and which on taste.

## Architecture

Rules are implemented natively in each language's own analyzer framework. The six contracts are implemented once, in a core binary that reads only interchange files.

&#91;embedded content: gate architecture · 3 native packages, 1 core\]

Each native package writes its findings, graph, function units and test data to `.gate/out`. The core reads those files and the protected files, and decides the exit code.

| Lives in the native package | Lives in the core |
| --- | --- |
| The deterministic rules from the rule spec | Change record and architecture delta |
| Dependency graph extraction from the language's semantic model | Trace check |
| Normalized hashing of functions | Test admission and pruning |
| Running the language's mutation tool and test runner | Verdict lockfile and judge runner |
| Mapping code to components | Hardening, conflict handling and the exit code |

A native package may wrap an existing linter for a rule, provided the result passes the conformance corpus. It adds no rules of its own.

The first release is built for three languages.

| Language | Rules hosted in | Graph from | Mutation tool |
| --- | --- | --- | --- |
| C# | A Roslyn analyzer | The Roslyn semantic model | Stryker.NET |
| TypeScript | typescript-eslint rules | The TypeScript type checker | StrykerJS |
| Python | A standalone checker on the `ast` module | A type checker in strict mode (pyright or mypy) | mutmut or cosmic-ray, through a converter to the report schema |

Python is dynamically typed, so its graph is only as complete as its type annotations. The Python package requires strict type checking, and a call whose target cannot be resolved is a blocking finding, not a missing edge.

## Rule spec

Each rule below is the single definition that every native package implements. Basis says what the default rests on: **evidence** (a study supports it), **taste** (no empirical support found) or **design** (proposed here, not found in any existing tool).

A function means any named or anonymous callable with a body. SLOC means source lines excluding blank lines and comments. Production code excludes test projects and generated code.

### Size and shape

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| SZ01 | Function body SLOC must not exceed the limit | 30 | block | evidence: methods of 24 SLOC or fewer were less bug-prone in a study of 785,606 Java methods; 20 to 40 is the supported band |
| SZ02 | Cognitive complexity of a function must not exceed the limit | 15 | block | evidence: the only complexity metric with a published comprehension validation |
| SZ03 | Nesting depth of control structures in a function must not exceed the limit | 3 | block | taste |
| SZ04 | A function must not declare more parameters than the limit | 4 | block | taste |
| SZ05 | Type SLOC must not exceed the limit | 200 | warn | taste |

### Arguments

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| AR01 | Flag parameter: a boolean or enum parameter must not be the condition of a branch in the function body | on | block | taste |
| AR02 | Output parameter: a function must not return data by writing through a parameter. Exempt: the language's standard try-parse pattern | on | block | taste |

### Side effects

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| FX01 | Effect APIs (clock, file system, network, console, randomness, environment, mutable global state) may be called only inside components marked as effect components in the architecture model | on | block | design: replaces purity inference, which no C# tool does soundly |
| FX02 | Command-query separation: a function that returns a value must not assign to instance, static or module state | on | warn | design: heuristic, expect false positives on caches and builders |
| FX03 | A function must not mutate an object it received as an argument | on | warn | design: heuristic |

### Duplication

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| DU01 | A change must not introduce a statement sequence whose normalized hash equals another sequence in production code | 6 statements | block on new clones, report on existing | evidence: duplication is the dominant measured failure of unconstrained agent code |

### Error handling

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| ER01 | A catch block must not be empty and must not only log | on | block | taste |
| ER02 | Null must not be returned or passed where the type system can express absence. The language's strict null checking is enabled with its warnings as errors | on | block | taste |
| ER03 | A function with side effects must not signal failure through a boolean or integer return value | on | report | design: heuristic, needs measurement |

### Coupling

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| CP01 | Law of Demeter: a member-access chain must not pass through more than one behavioural object defined in the solution. Exempt: data records, standard-library types, fluent calls returning the receiver's type | 1 | warn | taste |

### Dead code

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| DC01 | A non-public symbol must be referenced by production code | on | block | taste |
| DC02 | Comments must not contain code | on | block | taste |
| DC03 | A numeric literal other than -1, 0 and 1 must be a named constant in production code | on | warn | taste |

### Fragmentation

These are the brakes on the size rules. FR03 is measured per change, because absolute limits here are cheap to game.

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| FR01 | An abstraction with exactly one production implementation and exactly one production consumer must sit on a boundary declared in the architecture model | on | warn | design |
| FR02 | Pass-through: a function whose body is one call that forwards at least 80% of its parameters | 80% | warn | design: the 80% test comes from a Rust crate, self-declared heuristic |
| FR03 | Extract balance: single-caller functions added minus single-caller functions removed in one change must not exceed the limit | 5 | warn | evidence: agents under refactoring pressure extracted 14 to 226 times more than they inlined |
| FR04 | Indirection depth: in-solution call frames from an entry point to the first effect call | 6 | report | design: needs measurement |

### Naming

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| NM01 | Glossary: an identifier must not use a word the glossary lists as a banned synonym of a domain term | on | warn | design |

### Tests

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| TS01 | A test must contain at least one assertion | on | block | taste |
| TS02 | A test must not assert only on values it configured a test double to return | on | block | design |
| TS03 | DU01 applied to test code | 4 statements | block | design |

### Hardening

| ID | Rule | Default | Severity | Basis |
| --- | --- | --- | --- | --- |
| HD01 | An inline suppression must name the rule ID, a reason, an expiry date and an approver | on | block | design |
| HD02 | A change by the implementer must not touch a protected path | on | block | evidence: agents edit tests, baselines and configs to pass |
| HD03 | A change must not modify or delete existing tests unless its change record declares it | on | block | evidence: test modification was the main cheating method for some models |

## Contracts

The six contracts are implemented once, in the core, and work only on graphs, hashes and reports. They are the part of the gate that no existing tool was found to ship.

### C1 Change record

Every change carries a `change.yaml`, written and approved before implementation starts.

- **Reason:** exactly one, from a fixed taxonomy (feature, fix, refactor, dependency, performance, compliance), plus the actor who asked for it.
- **Architecture delta:** edges added and edges removed (C2).
- **Declared test changes:** existing tests the change will modify or delete, each with a reason (HD03).
- A change that needs two reasons is two changes. The core rejects a record with more than one.
- Approval is recorded by someone other than the implementer. The implementer cannot edit the record after approval.

### C2 Architecture delta

The model is `architecture.yaml` in the repo: components, a mapping from code to components, the allowed edges between them, and which components are effect components and boundaries.

- **Closed world:** every production type maps to exactly one component. An unmapped type blocks.
- **Observed delta:** the dependency graph at head minus the graph at base, from the native extractors.
- **Check:** observed delta must equal the declared delta. An undeclared edge blocks. A declared edge that the change never adds also blocks, so the model cannot drift ahead of the code.
- **Order:** the commit that changes `architecture.yaml` and `change.yaml` precedes every implementation commit in the change.

### C3 Trace check

Integration tests run with tracing, and every span carries its component name.

- An observed edge between components that the model does not allow blocks. This catches edges through dependency injection, reflection and messaging that the static graph misses.
- A declared edge that no test exercised is a report, not a failure. In the one industrial study found, 88% scenario coverage coexisted with 34% statement coverage, so this signal mostly measures test coverage.
- Runtime-generated types (proxies, closures, async state machines) are mapped to the component of the type they wrap, or the check produces false violations.

### C4 Test admission

A test is admitted on what it adds, not on its existence.

- **Marginal kill:** a new test must kill at least one mutant on the changed lines that no existing test kills. Otherwise it is rejected as redundant.
- **Survivors:** every surviving mutant on changed, covered lines must be killed or waived by a human. There is no score threshold.
- **Ignore list:** mutants in logging, tracing and argument guards are excluded. The list is a protected path.
- **Runtime budget:** test wall-clock time added by one change must not exceed the budget. The default is an open question.
- **Selection:** a change runs only the tests that cover its changed code. The full suite runs nightly.
- **Pruning:** the nightly run produces the full matrix of which test kills which mutant. A test that kills nothing, or whose kills are a subset of another test's, is listed for deletion.
- Mutation testing does not catch code that special-cases the tested inputs. Property-based tests are required for components the model marks as core logic.

### C5 Verdict lockfile

The judge is advisory in this version. The contract exists so its verdicts are reproducible when a question is later promoted.

| Question ID | Question |
| --- | --- |
| J01 | Does a name suggested from the body alone match the function's actual name? |
| J02 | Are all statements in the function at one level of abstraction? |
| J03 | Can a meaningfully named function be extracted from this one? |
| J04 | Does this comment restate the code or contradict it? |
| J05 | Does this parameter or field use a primitive where a domain type exists or is warranted? |
| J06 | Does this layer add behaviour beyond forwarding? |

- **Key:** function hash, question ID, rubric version, judge fingerprint (model ID, prompt hash, vote count).
- **Minting:** only in CI, by an odd number of votes (default 5). The vote split is stored. A non-unanimous vote is recorded as pass with a note and never as fail.
- **Prompt:** zero-shot. A rationale is generated only after a fail verdict and is never part of the verdict. In one study, asking for an explanation raised false rejection of correct code from 26% to 73% or more.
- **J01** is asked as suggest-then-compare, which was more stable across runs than direct judging.
- **Storage:** the committed lockfile holds pass verdicts. Fail history is kept outside the repo, so an edit that only changes the hash cannot re-roll a fail.
- **Promotion:** a question becomes blocking only after it is measured on a labelled set from the target codebase. Set size and the acceptable false-rejection rate are open questions.
- **Model retirement:** existing verdicts stay valid. Only new or changed functions are judged by the new model.

### C6 Hardening

- **Protected paths:** `gate.yaml`, `architecture.yaml`, approved `change.yaml` files, baselines, ignore lists, the lockfile, the conformance corpus, CI definitions and linter severity settings. The core compares the change's paths against the list and blocks on a match from the implementer identity.
- **Suppressions:** the only accepted form is HD01. Compiler-level and linter-level disable switches are violations.
- **Conflict:** `gate conflict` files a record that the checks and the task cannot both be satisfied, and stops the change for a human. In ImpossibleBench an explicit exit of this kind cut one model's cheating from 54% to 9%.
- **Hooks:** agent hooks give fast feedback only. CI is the gate, because hooks can fail open.
- **Single-responsibility report:** from the change records, the core counts distinct reasons and actors per component over a trailing window. It is a report only; nothing validates it against defects yet.

## Interchange formats

A native package talks to the core only through the six files below, written to `.gate/out/<language>/`. Existing standards are used where one exists.

| Artefact | Format | Content |
| --- | --- | --- |
| Findings | SARIF 2.1.0 | One result per violation, rule ID from this spec, message in the diagnostic format below |
| Dependency graph | `graph.json` (schema in this spec) | Nodes: symbol ID, kind, component. Edges: from, to, kind (call, construct, inherit, type reference) |
| Function units | `units.json` (schema in this spec) | Symbol ID, file, line span, SLOC, normalized hash, statement-sequence hashes |
| Mutation results | Stryker mutation testing report schema | Per mutant: location, status, and which tests cover and kill it |
| Traces | OpenTelemetry spans (OTLP file export) | Attribute `gate.component` on every span |
| Test results | JUnit XML | Test ID and duration |

A SCIP index is accepted in place of `graph.json` for a language that has an indexer but no native package yet. It is coarser, so C2 runs at warn for that language.

**Symbol IDs** are stable across runs and unique per language: language, package or assembly, fully qualified name, and parameter types.

**Normalized hash.** The hash is what DU01, TS03 and the lockfile key rely on, so its definition is fixed:

1. Take the syntax tree of the function body or statement sequence.
2. Drop comments and whitespace.
3. Replace each identifier with a placeholder numbered by first occurrence.
4. Replace each literal with a placeholder for its type.
5. Keep operators, control structure, call arity and declared types.
6. Serialize the tree in a fixed order and take SHA-256.

Hashes are compared within one language only.

**Diagnostic format.** Every message has three parts, because the reader is usually an agent: the rule ID, what is wrong with the measured value and the limit, and what to do. Example: `SZ01: function Parse has 47 SLOC, limit 30. Split it at a point where the extracted part has a name that is not a restatement of its body.`

## CLI

The installer and the core are one binary, `gate`, so there is one thing to version and pin.

| Command | What it does |
| --- | --- |
| `gate init` | Detects languages, installs the native package for each at a pinned version, writes `gate.yaml`, generates `architecture.yaml` from the current graph for a human to edit, adds the CI job, the protected-path rules and the agent hooks |
| `gate check` | Runs every stage on the current change. `--stage rules`, `delta`, `tests`, `trace` or `judge` runs one |
| `gate change new` | Creates a `change.yaml` skeleton |
| `gate change approve` | Records approval. Refused for the implementer identity |
| `gate tests prune` | Prints the deletion candidates from the latest nightly kill matrix |
| `gate judge mint` | Mints verdicts for unjudged hashes. CI only |
| `gate conflict "<text>"` | Files a conflict record and exits with code 3 |
| `gate explain <rule ID>` | Prints the rule's definition, default and fix guidance |
| `gate doctor` | Reports version skew between core, native packages, spec and corpus |

**Exit codes**

| Code | Meaning |
| --- | --- |
| 0 | Pass |
| 1 | At least one blocking violation |
| 2 | The gate itself failed or is misconfigured. Treated as a failure, never as a pass |
| 3 | Conflict filed; the change waits for a human |

**Config.** One file for all languages. Thresholds are set once and apply everywhere.

```yaml
spec: 1.0
languages: [csharp, typescript, python]
implementers: [agent-bot]
rules:
  SZ01: { limit: 30 }
  SZ04: { limit: 4 }
  FR03: { limit: 5, severity: warn }
tests:
  runtime_budget_seconds: 5
  mutation_ignore: .gate/mutation-ignore.yaml
judge:
  mode: advisory
  votes: 5
protected:
  - gate.yaml
  - architecture.yaml
  - .gate/**
```

**Installer rules**

- `gate init` is idempotent. A second run changes nothing unless versions moved.
- It never lowers an existing setting. If the repo already has a stricter linter rule, the stricter one stays.
- It turns on the language's strict modes that the rules depend on (nullable reference types as errors in C#, `strict` in TypeScript, a type checker in strict mode in Python) and reports existing violations as a baseline for a human to accept.
- A baseline is a protected file. Violations in it are reported; new ones block.

## Conformance corpus

A native package cannot be released unless it produces exactly the expected diagnostics on every corpus case for its language. The corpus is what stops the per-language implementations from drifting apart.

**Structure of a case**

- A language-neutral description: the rule ID, what the code does, and whether it violates.
- One fixture file per language expressing that same case.
- The expected result per fixture: rule ID and line, or none.

**Minimum per rule**

| Kind | Count | Purpose |
| --- | --- | --- |
| Violating | 3 | The rule fires |
| Conforming | 3 | The rule stays silent, including one case just under the limit |
| Edge | 2 | Exemptions and constructs that differ between languages (lambdas, local functions, generated code, pattern matching) |

**Additional suites**

- **Hash suite:** pairs of fragments that must hash equal (renamed identifiers, changed literals, reformatted) and pairs that must differ (changed operator, changed call arity, reordered statements).
- **Graph suite:** small fixture projects with the expected `graph.json`, covering dependency injection, generics, inheritance and source-level references such as `nameof`.
- **Contract suite:** fixture changes with a `change.yaml` and the expected verdict from the core. It is language-neutral and runs against the core only.

**Rules for the corpus**

1. A case is written in the neutral description first. Fixtures follow.
2. If two languages cannot agree on a case, the rule definition is ambiguous. The spec is fixed before any package is.
3. A bug found in a native package becomes a corpus case before it is fixed.
4. The corpus is a protected path, versioned with the spec.

**Versioning.** The spec has a semantic version. Each native package declares the spec version it implements. `gate doctor` refuses to run packages on different major versions together.

## Self-hosting

The gate's own repositories run the gate at blocking severity on every rule, from the first commit, with no suppressions and no baseline.

**What it covers**

- The core, every native package, the installer and the corpus harness.
- The gate's own `architecture.yaml` and change records, checked by C1 and C2.
- The gate's own tests, admitted under C4.
- One exemption: corpus fixture files, which contain violations on purpose. They live under one protected path and are excluded by path, not by suppression.

**Obvious code.** The gate's code must be readable by someone holding only this spec. These requirements apply to the gate itself, on top of the rule table:

1. **One rule, one file, one entry function.** The file is named after the rule ID. The entry function takes a function unit or a graph and returns findings.
2. **The definition is in the file.** Each rule file opens with the rule ID and its definition sentence copied from this spec. The corpus harness fails if the sentence differs from the spec.
3. **The code reads in the order of the definition.** Measure, compare to the limit, build the message.
4. **No reflection, runtime code generation or dynamic dispatch by name** in rule code or in the core.
5. **No shared mutable state.** A rule is a function of its input. FX01 applies: only the reader and writer components touch the file system.
6. **No framework in the core** beyond a parser, a YAML reader and a hash function. Each dependency is listed in the architecture model with a reason.
7. **Warn-level rules are blocking here.** FX02, FX03, CP01, DC03, FR01, FR02 and FR03 block in the gate's own repositories.

**When a rule does not fit**

If the gate's own code cannot satisfy a rule without contortion, that is evidence against the rule. The rule or its default is changed in the spec, with the case recorded in the corpus. It is never suppressed locally.

**Bootstrap**

1. Version 0 has no gate to run. It is checked by hand against the rule table, and its rule files against the corpus.
2. From the first working native package, each version is built and checked by the previous released version.
3. A release is blocked if the candidate fails when run on itself.

## Build order and open questions

Hardening and test admission come first, because every later layer is void without the first and the second is the problem already hurting CI.

1. **Spec and corpus** for HD, TS, DU01 and SZ rules, in C#, TypeScript and Python.
2. **Hardening (C6):** protected paths, suppression format, `gate conflict`, change records (C1).
3. **Test admission (C4):** marginal kill, per-mutant survivors, test selection, nightly pruning.
4. **Hasher and DU01:** the normalized hash, shipped as the clone rule and reused for TS03 and the lockfile.
5. **Architecture delta (C2):** native graph extractors and the equality check.
6. **Remaining deterministic rules:** AR, FX, ER, CP, DC, NM.
7. **Trace check (C3).**
8. **Fragmentation signals:** FR01 to FR04.
9. **Judge (C5):** advisory, with the labelled set built in parallel.

**Open questions**

Each of these needs a decision or a measurement before the related rule can block.

- [ ] What does diff-scoped mutation testing cost per change in C#, TypeScript and Python? No runtime numbers were found for Stryker.NET on real projects.
- [ ] Is Stryker.NET's `since` filter file-level or line-level? C4 needs line-level.
- [ ] What is the default test runtime budget per change? The 5 seconds in the config example is a placeholder.
- [ ] What false-rejection rate does each judge question have on real C#, TypeScript and Python functions, and what rate is acceptable for blocking?
- [ ] How large must the labelled set be per question?
- [ ] Are architecture edges declared between interfaces or between implementations?
- [ ] How are message and event edges modelled: messages as components, or sender joined to handler on message type?
- [ ] Are the SZ04 limit of 4 and the FR03 limit of 5 right? Both are guesses.
- [ ] Which Python mutation tool is used, mutmut or cosmic-ray, and what does its converter to the report schema cost? Can the Python rules be hosted in an existing linter, or must they be a standalone checker?
- [ ] What is the product called?

## Sources

The evidence labels in the rule spec and contracts come from the research report "Clean code compiler gate" delivered earlier in this conversation. The studies cited by number in this doc:

- [Chowdhury et al., MSR 2022](https://www.cs.ubc.ca/~rtholmes/papers/msr_2022_chowdhury.pdf): method size and bug-proneness, 785,606 Java methods.
- [Muñoz Barón et al.](https://arxiv.org/abs/2007.12520): cognitive complexity and comprehension.
- [Jiang et al.](https://arxiv.org/pdf/2610.36346): agent extract-to-inline ratios under refactoring pressure.
- [Jin and Chen](https://arxiv.org/html/2603.00539): false rejection of correct code by LLM judges.
- [Nishiura et al., ENASE 2025](https://www.scitepress.org/publishedPapers/2025/133544/pdf/index.html): method-name judging, suggest-then-compare.
- [ImpossibleBench](https://arxiv.org/html/2510.20270v1): agents cheating on tests, and the effect of an explicit exit.
- [Petrović et al.](https://arxiv.org/pdf/2102.11378): mutation testing on changed lines at Google.
- [Meta ACH](https://arxiv.org/html/2501.12862v1): mutant-killing tests that add no coverage.
- [Nicolaescu et al., APSEC 2017](https://swc.rwth-aachen.de/docs/2017_APSEC_Nicolaescu.pdf): ARAMIS runtime conformance and the coverage finding.
- [Stryker.NET configuration](https://stryker-mutator.io/docs/stryker-net/configuration/): `since`, baseline and break thresholds.
