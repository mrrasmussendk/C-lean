using Gate.Collect;
using Gate.Effects;

namespace Gate.Cli;

/// <summary>`gate help`, and the next step the gate suggests after a command: what a newcomer runs next, and why.</summary>
internal static class Guide
{
    private const string Commands = """

        Set up a repository
          gate init                  write gate.yaml, a drafted architecture.yaml, the CI job and the agent hooks
          gate doctor                show which packages are installed and whether their versions agree

        Work on a change
          gate change new <id>       start the change record changes/<id>.yaml
          gate change approve <id>   approve it, by someone who is not an implementer
          gate check                 check the change against its base branch
          gate check --file <path>   check one file, right after an edit
          gate explain <rule ID>     what a rule asks for, and how to fix a finding

        More
          gate check --stage rules|delta|tests|trace|judge [--base <ref>] [--offline]
          gate stats | instructions | hook | tests nightly | tests prune | judge mint | conflict "<text>"

        Every command takes --root <dir>; without it the current directory is the repository.

        """;

    public static int Help(IReadOnlyList<string> arguments)
    {
        Surroundings.Print($"gate {DoctorCommand.CoreVersion} (spec {DoctorCommand.SpecVersion}): the clean code gate.");
        Surroundings.Print(Commands.ReplaceLineEndings("\n"));
        Surroundings.Print($"Next step here: {Next(Options.Value(arguments, Options.RootOption, Options.Here))}");
        return Contracts.C6Hardening.Passed;
    }

    public static int Version()
    {
        Surroundings.Print($"{DoctorCommand.CoreVersion} spec {DoctorCommand.SpecVersion}");
        return Contracts.C6Hardening.Passed;
    }

    /// <summary>The one command that moves this repository forward from where it is.</summary>
    public static string Next(string root) =>
        !Files.Exists(Path.Combine(root, Layout.Config)) ? "run `gate init` in the root of the repository you want gated."
        : Files.Count(Path.Combine(root, Layout.Records)) == 0 ? "run `gate change new <id>` to describe your first change, then `gate check`."
        : "run `gate check` to check your change.";

    /// <summary>What a human does after `gate init`, in order. Printed every time, so a second run repeats the list.</summary>
    public static IReadOnlyList<string> AfterInit(IReadOnlyList<string> languages) =>
    [
        "",
        "Next steps:",
        $"  1. {Layout.Config}: set each language's test command under tests.commands ({Listed(languages)}).",
        "     It must write JUnit XML to $GATE_JUNIT. Without it the tests stage has nothing to run.",
        $"  2. {Layout.Architecture}: review the drafted components and the dependencies allowed between them.",
        $"  3. {Layout.Baseline}: accept the existing violations listed there, or fix them. New ones block.",
        "  4. Commit what was written. These files are protected from the implementer.",
        "  5. Start a change: `gate change new <id>`, fill it in, then `gate check`.",
        "",
        "`gate doctor` shows what is installed; `gate help` lists every command.",
    ];

    /// <summary>What stands between this repository and a working gate, one line per obstacle; empty when nothing does.</summary>
    public static IReadOnlyList<string> Obstacles(string root, IReadOnlyList<string> missing) =>
        new[]
        {
            IsRepository(root) ? "" : "note: this directory is not a git repository. `gate check` compares a change with its base branch; run `git init` and commit first.",
        }
        .Concat(missing.Select(Missing))
        .Where(line => line.Length > 0)
        .ToList();

    /// <summary>How to install the package of a language whose command did not answer.</summary>
    public static string Missing(string language) =>
        language == "csharp"
            ? "note: gate-csharp is not installed. Install it with `dotnet tool install --global CLean.Gate.CSharp`, or run the gate's install.sh."
            : $"note: gate-{language} is not installed. Run the gate's install.sh; it installs the package of every language whose toolchain is on this machine.";

    public static string AfterNewChange(string id) =>
        $"Next: fill in `acceptance` and `delta` in that file, have it approved with `gate change approve {id}`, then run `gate check`.";

    private static string Listed(IReadOnlyList<string> languages) =>
        languages.Count == 0 ? "none detected yet" : string.Join(", ", languages);

    private static bool IsRepository(string root) =>
        Files.Exists(Path.Combine(root, GitDirectory)) || Files.Count(Path.Combine(root, GitDirectory)) > 0;

    private const string GitDirectory = ".git";
}
