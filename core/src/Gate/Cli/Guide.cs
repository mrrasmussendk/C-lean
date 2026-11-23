using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;

namespace Gate.Cli;

/// <summary>One thing a repository needs before the gate works in it: whether it is there, and the command that puts it there.</summary>
internal sealed record SetupStep(bool Done, string What, string How);

/// <summary>`gate help`, and the next step the gate suggests after a command: what a newcomer runs next, and why.</summary>
internal static class Guide
{
    private static readonly IReadOnlyList<(string Heading, string Lines)> Sections =
    [
        ("Set up a repository", """
              gate init                  write gate.yaml, a drafted architecture.yaml, the CI job and the agent hooks
              gate doctor                show which packages are installed and whether their versions agree
            """),
        ("Work on a change", """
              gate change new <id>       start the change record changes/<id>.yaml
              gate change approve <id>   approve it, by someone who is not an implementer
              gate check                 check the change against its base branch
              gate check --file <path>   check one file, right after an edit
              gate explain <rule ID>     what a rule asks for, and how to fix a finding
            """),
        ("More", """
              gate check --stage rules|delta|tests|trace|judge [--base <ref>] [--offline]
              gate stats | instructions | hook | tests nightly | tests prune | judge mint | conflict "<text>"
            """),
    ];

    public static int Help(IReadOnlyList<string> arguments)
    {
        var pen = Pen();
        var caption = $"the clean code gate, version {DoctorCommand.CoreVersion} (spec {DoctorCommand.SpecVersion})";
        PrintAll(Surroundings.Watched() ? [.. Art.Banner(pen, caption), ""] : [$"C-Lean: {caption}.", ""]);
        foreach (var (heading, lines) in Sections)
        {
            PrintAll([pen.Paint(heading, Art.Bold), lines.ReplaceLineEndings("\n").TrimEnd('\n'), ""]);
        }

        PrintAll(["Every command takes --root <dir>; without it the current directory is the repository.", ""]);
        PrintAll(Progress(Options.Value(arguments, Options.RootOption, Options.Here), pen));
        return C6Hardening.Passed;
    }

    public static int Version()
    {
        Surroundings.Print($"{DoctorCommand.CoreVersion} spec {DoctorCommand.SpecVersion}");
        return C6Hardening.Passed;
    }

    /// <summary>The setup checklist of this repository, and under it the one command that moves it forward.</summary>
    public static IReadOnlyList<string> Progress(string root, Pen pen)
    {
        var steps = Steps(root);
        var next = steps.Where(step => !step.Done).Select(step => step.How).FirstOrDefault("gate check");
        return
        [
            pen.Paint($"Setup in this repository ({steps.Count(step => step.Done)} of {steps.Count} done)", Art.Bold),
            .. steps.Select(step => step.Done ? Art.Ticked(pen, step.What) : Art.Unticked(pen, step.What, step.How)),
            "",
            $"{pen.Paint("Next step:", Art.Bold)} {next}",
        ];
    }

    /// <summary>What a human still edits after `gate init`; the checklist cannot see whether these were reviewed.</summary>
    public static IReadOnlyList<string> AfterInit(string root, Pen pen) =>
    [
        "",
        .. Progress(root, pen),
        "",
        "Also review, then commit; these files are protected from the implementer:",
        $"  {Layout.Architecture}: the drafted components and the dependencies allowed between them.",
        $"  {Layout.Baseline}: the existing violations, to accept or to fix. New ones block.",
        "",
        "`gate doctor` shows what is installed; `gate help` lists every command.",
    ];

    /// <summary>How to install the package of a language whose command did not answer.</summary>
    public static string Missing(string language) =>
        language == "csharp"
            ? "note: gate-csharp is not installed. Install it with `dotnet tool install --global CLean.Gate.CSharp`, or run the gate's install.sh."
            : $"note: gate-{language} is not installed. Run the gate's install.sh; it installs the package of every language whose toolchain is on this machine.";

    public static string AfterNewChange(string id) =>
        $"Next: fill in `acceptance` and `delta` in that file, have it approved with `gate change approve {id}`, then run `gate check`.";

    /// <summary>Colours for a person watching a terminal, none for anyone else.</summary>
    public static Pen Pen() => Surroundings.Watched() ? Cli.Pen.Colours : Cli.Pen.Plain;

    private static IReadOnlyList<SetupStep> Steps(string root)
    {
        var configured = Files.Exists(Path.Combine(root, Layout.Config));
        var config = configured ? InputReader.Config(root) : InputReader.Unpinned;
        var untested = config.Languages.Where(language => !config.Tests.Commands.ContainsKey(language)).ToList();
        return
        [
            new(configured, $"{Layout.Config} written", "gate init"),
            new(Files.Exists(Path.Combine(root, Layout.Architecture)), $"{Layout.Architecture} drafted", "gate init"),
            new(IsRepository(root), "inside a git repository", "git init, then commit"),
            new(configured && untested.Count == 0, "test command set for each language", $"edit tests.commands in {Layout.Config}{Named(untested)}"),
            new(Files.Count(Path.Combine(root, Layout.Records)) > 0, "first change described", "gate change new <id>"),
        ];
    }

    private static string Named(IReadOnlyList<string> languages) =>
        languages.Count == 0 ? "" : $" ({string.Join(", ", languages)})";

    private static bool IsRepository(string root) =>
        Files.Exists(Path.Combine(root, GitDirectory)) || Files.Count(Path.Combine(root, GitDirectory)) > 0;

    private static void PrintAll(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            Surroundings.Print(line);
        }
    }

    private const string GitDirectory = ".git";
}
