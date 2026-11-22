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
