namespace Gate.Model;

internal sealed record RuleEntry(string Id, string Definition, string Default, string Guidance)
{
    public required Severity Severity { get; init; }
}

/// <summary>The rule table of the spec, for `gate explain` and for defaults. Kept equal to spec/rules.yaml by the corpus harness.</summary>
internal static class RuleTable
{
    public static readonly IReadOnlyList<RuleEntry> Entries =
    [
        Block("SZ01", "Function body SLOC must not exceed the limit", "30", "Split the function at a point where the extracted part has a name that is not a restatement of its body."),
        Block("SZ02", "Cognitive complexity of a function must not exceed the limit", "15", "Replace nested conditions with guard clauses, or move a whole decision into a named function."),
        Block("SZ03", "Nesting depth of control structures in a function must not exceed the limit", "3", "Return early, or move the inner structure into a named function."),
        Block("SZ04", "A function must not declare more parameters than the limit", "4", "Group parameters that travel together into one type."),
        Warn("SZ05", "Type SLOC must not exceed the limit", "200", "Move one responsibility of the type into a type of its own."),
        Block("AR01", "Flag parameter: a boolean or enum parameter must not be the condition of a branch in the function body", "on", "Write one function per branch and let the caller choose."),
        Block("AR02", "Output parameter: a function must not return data by writing through a parameter. Exempt: the language's standard try-parse pattern", "on", "Return the data, as a record if there are several values."),
        Block("FX01", "Effect APIs (clock, file system, network, console, randomness, environment, mutable global state) may be called only inside components marked as effect components in the architecture model", "on", "Move the call into an effect component and pass its result in."),
        Warn("FX02", "Command-query separation: a function that returns a value must not assign to instance, static or module state", "on", "Split it into a command that changes state and a query that returns the value."),
        Warn("FX03", "A function must not mutate an object it received as an argument", "on", "Build and return a new object."),
        Block("DU01", "A change must not introduce a statement sequence whose normalized hash equals another sequence in production code", "6 statements", "Call the existing code, or extract the shared sequence into one function used by both places."),
        Block("ER01", "A catch block must not be empty and must not only log", "on", "Handle the error, rethrow it, or do not catch it."),
        Block("ER02", "Null must not be returned or passed where the type system can express absence. The language's strict null checking is enabled with its warnings as errors", "on", "Declare the type as optional, or return an empty value of the type."),
        Report("ER03", "A function with side effects must not signal failure through a boolean or integer return value", "on", "Throw, or return a result type that names the failure."),
        Warn("CP01", "Law of Demeter: a member-access chain must not pass through more than one behavioural object defined in the solution. Exempt: data records, standard-library types, fluent calls returning the receiver's type", "1", "Ask the first object for what you need and let it talk to its own collaborators."),
        Block("DC01", "A non-public symbol must be referenced by production code", "on", "Delete the symbol."),
        Block("DC02", "Comments must not contain code", "on", "Delete the commented-out code; version control keeps it."),
        Warn("DC03", "A numeric literal other than -1, 0 and 1 must be a named constant in production code", "on", "Give the number a name that says what it means."),
        Warn("FR01", "An abstraction with exactly one production implementation and exactly one production consumer must sit on a boundary declared in the architecture model", "on", "Use the implementation directly, or declare the boundary in architecture.yaml."),
        Warn("FR02", "Pass-through: a function whose body is one call that forwards at least 80% of its parameters", "80%", "Call the target directly and delete the forwarding function."),
        Warn("FR03", "Extract balance: single-caller functions added minus single-caller functions removed in one change must not exceed the limit", "5", "Inline the extractions that only restate their body."),
        Report("FR04", "Indirection depth: in-solution call frames from an entry point to the first effect call", "6", "Remove layers that only forward."),
        Warn("NM01", "Glossary: an identifier must not use a word the glossary lists as a banned synonym of a domain term", "on", "Rename the identifier to the glossary's term."),
        Block("TS01", "A test must contain at least one assertion", "on", "Assert on the observable result of the code under test."),
        Block("TS02", "A test must not assert only on values it configured a test double to return", "on", "Assert on what the production code did with the value."),
        Block("TS03", "DU01 applied to test code", "4 statements", "Move the repeated arrangement into one helper or a parameterized test."),
        Block("HD01", "An inline suppression must name the rule ID, a reason, an expiry date and an approver", "on", "Use `gate:allow <RULE-ID> reason=\"...\" expires=<YYYY-MM-DD> approver=<name>`, or fix the finding."),
        Block("HD02", "A change by the implementer must not touch a protected path", "on", "Revert the edit. If the task cannot be done without it, run `gate conflict`."),
        Block("HD03", "A change must not modify or delete existing tests unless its change record declares it", "on", "Restore the test, or have the change record declare it before implementation."),
        Block("GR01", "A call whose target cannot be resolved is a blocking finding, not a missing edge", "on", "Annotate the receiver's type so the call resolves."),
    ];

    private static RuleEntry Block(string id, string definition, string limit, string guidance) =>
        new(id, definition, limit, guidance) { Severity = Severities.Block };

    private static RuleEntry Warn(string id, string definition, string limit, string guidance) =>
        new(id, definition, limit, guidance) { Severity = Severities.Warn };

    private static RuleEntry Report(string id, string definition, string limit, string guidance) =>
        new(id, definition, limit, guidance) { Severity = Severities.Report };
}
