using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;

namespace Gate.Cli;

/// <summary>`c-lean rule new`: starts a rule of the repository as a file that is complete, valid and switched off.</summary>
internal static class RuleScaffold
{
    private const string Template = """
        # A rule of this repository. One file per rule; the file is named after the rule ID.
        # `c-lean check` judges it on the lines a change touches. `c-lean rules` lists it.
        id: {id}

        # One sentence: what the rule asks for.
        definition: A comment must not hold a TODO

        # block   the change cannot merge
        # warn    a human must acknowledge it
        # report  recorded, never stops a change
        # off     not enforced. A new rule starts off: set one of the others when it is written.
        severity: off

        # The files the rule applies to. Folders in .c-leanignore are left alone.
        files: ["**/*.cs", "**/*.ts", "**/*.py"]

        # How the rule finds a violation. Give exactly one of `pattern` and `command`.
        #
        # pattern: a regular expression. Every line it matches is a violation, reported with `message`.
        pattern: '\bTODO\b'
        message: this line holds a TODO

        # command: a program, run in the repository root. It reads the files to judge from standard
        # input, one per line, prints `path:line: message` for each violation, and exits 0 or 1.
        # command: tools/my-rule

        # What to do about a finding.
        fix: Do the work now, or file it where work is tracked and delete the comment.

        """;

    public static int New(IReadOnlyList<string> arguments, IReadOnlyList<string> words)
    {
        var id = words.Count > 0 ? words[0] : throw new GateFailure("the rule ID is missing. Usage: c-lean rule new <ID>, as in c-lean rule new NO-TODO");
        var path = Path.Combine(Options.Value(arguments, Options.RootOption, Options.Here), Layout.Rules, $"{id}.yaml");
        if (Files.Exists(path))
        {
            throw new GateFailure($"{path}: a rule with this ID exists. Edit that file, or choose another ID.");
        }

        var problems = Parse.RuleText.Problems(id, Template.Replace("{id}", id));
        if (problems.Count > 0)
        {
            throw new GateFailure(string.Join("\n      ", problems.Select(problem => problem[(problem.IndexOf(' ') + 1)..])));
        }

        Files.Write(path, Template.Replace("{id}", id));
        Surroundings.Print($"wrote {path}");
        Surroundings.Print("Next: describe the rule in that file and set its severity. It is off until you do. `c-lean rules` lists it.");
        return C6Hardening.Passed;
    }
}
