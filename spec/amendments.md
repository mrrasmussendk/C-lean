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
