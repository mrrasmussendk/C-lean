# Amendments found by self-hosting

"If the gate's own code cannot satisfy a rule without contortion, that is evidence
against the rule. The rule or its default is changed in the spec, with the case
recorded in the corpus." Each entry below is such a case. `spec.md` itself is left as
written until its author accepts the change.

## FR02 exempts effect wrappers

FX01 allows effect APIs only inside effect components, so the rest of the code reaches
the console, the file system or the clock through a function there. The thinnest such
function is one call that forwards its parameters: exactly what FR02 forbids. The core,
the harness and all three packages hit this.

**Change:** a function in an effect component whose one call is an effect API is not a
pass-through. Corpus case: `FR02/e3-effect-wrapper`.

## Open: readings that differ between packages

Found while building the three packages. Each needs a neutral corpus case before it is
settled; until then the packages may disagree on code the corpus does not cover.

- **FR02 and anonymous functions.** Applied literally, FR02 flags every callback of
  the form `x => f(x, captured)`. The TypeScript package exempts anonymous functions;
  the others have not been checked against such a case.
- **FR02 and one-line predicates.** `IsX(a) => list.Contains(a)` is a pass-through by
  the definition. The C# package and the core inlined theirs.
- **DC03 and constant tables.** "The whole initializer of a named constant" makes each
  number inside a named table (`{SZ01: 30}`) a finding. All three packages read it
  strictly.
- **DC02 and prose.** "return early" parses as code in TypeScript; that package
  special-cases a bare-name return.
- **GR01.** "Cannot be resolved" is undefined for a receiver whose type cannot be
  inferred. The Python package is silent there and writes no edge.
- **AR02.** Python and TypeScript have no try-parse pattern, so nothing is exempt.
- **Graph.** A call on an abstraction is an edge to its method node in C# and Python
  and to the abstraction node in TypeScript.

## Additions the spec did not have

Made so that the gate needs no other tool; each is specified in `native-contract.md`.

- **`mutants` and `instrument`** on every native package, and the mutation run, test
  selection and trace collection in the core. The spec named Stryker.NET, StrykerJS,
  mutmut and OpenTelemetry; none is used. The mutation report is still written in the
  Stryker schema and traces as OTLP spans, so those tools' output remains readable.
- **Coverage without a coverage tool.** Mutants are compiled in behind a switch, and
  the switched code reports which mutants a run reaches. A mutant no test reaches is
  `NoCoverage` and is reported, not blocked, as the spec's "changed, covered lines"
  requires. Where a package cannot switch a mutant, it is applied by replacing bytes
  and then cannot be told apart from an uncovered one: it blocks if it survives.
- **Test selection from the static call graph.** A test that reaches changed code only
  through a dynamic call is not selected, which shows as a surviving mutant, not a pass.
- **`gate tests nightly`**, `tests.commands` and `tests.setup` in `gate.yaml`, the
  `context` of a mutant (logging, guard) for the ignore list, and `core_logic: true`
  on a component.
- **The trace check sees component edges, not spans.** The instrumenter records each
  distinct pair once; the core writes one parent and one child span per pair.

## Changes from the first real comparison

An agent wrote a bowling game under the gate and another without it. The gated code
passed every rule; reading it showed four things the rules got wrong or missed.

- **DC03 allows a named constant table.** A rack of pins written as data,
  `PIN_ROWS = ((7, 8, 9, 10), (4, 5, 6), (2, 3), (1,))`, tripped DC03, and the agent
  replaced it with a nine-line function that computes it. A number that is an element
  of a collection literal in a named constant's initializer is now named by the
  constant. Cases `DC03/e3-constant-table`, `DC03/v4-table-in-a-variable`.
- **ER01 is not satisfied by leaving.** A handler that printed and then did a bare
  `return` passed, while the same handler without the `return` was flagged. Statements
  that only transfer control without a value no longer count as handling. This is
  stricter and will flag some honest retry loops; the fix there is to return a value
  the caller checks. Case `ER01/v4-log-then-leave`.
- **SZ02 counts comprehension and query clauses.** Logic packed into nested generator
  expressions was invisible to the complexity measure. Cases
  `SZ02/v4-comprehension-clauses`, `SZ02/e3-one-comprehension`.
- **SZ06, line length, is new.** SZ01 counts lines, so long lines were the way under
  it. As a new rule it ships at report level (limit 120) and is promoted only after it
  has been measured.
- **The change record carries acceptance criteria.** The gated game was correct, far
  too easy and missing features, and nothing showed that. `acceptance:` in
  `change.yaml` lists what the change must do; it is covered by the approval digest and
  `gate check` prints each item for the reviewer. The gate does not decide them.

## Design: where the code must flex

Two agents wrote the same bowling game, one under the gate and one without. The gated
code had no rule findings and killed every mutant; the ungated code had the better
design (a type owning the game's state, collaborators passed in). Every rule the gate
had was a prohibition on local shape, and several lean against structure. A rule that
demanded patterns would get them everywhere, so the gate now enforces design only where
a human has said the design must flex, and reports the symptoms elsewhere.

- **Varying components.** `varies: true` on a component in `architecture.yaml` says its
  behaviour must be replaceable. Code outside it may call into it only through an
  abstraction defined there, and may construct its implementations only from a
  component marked `wires: true`. A varying component that defines no abstraction is a
  finding. Enforced from the graph by the core (`C2.variation`, blocking). FR01 does not
  warn about abstractions in a varying component.
- **DS01, repeated discriminator**, and **DS02, data clump**: the two symptoms seen in
  the gated bowling code (the tenth-frame case decided in three functions; aim, power
  and drift travelling together). New rules, so report level.
- **Change spread.** `gate check` reports how many components the changes of the
  trailing window touched. Poor scaling shows there as changes that keep getting wider.
- **Instructions.** The generated agent section lists the varying components and says
  what is expected at them, and asks for a type where functions share data and for
  collaborators to be passed in.
