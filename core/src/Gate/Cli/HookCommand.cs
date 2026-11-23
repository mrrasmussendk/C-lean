using System.Text.Json;
using Gate.Contracts;
using Gate.Effects;
using Gate.Parse;

namespace Gate.Cli;

/// <summary>
/// `c-lean hook`: the agent hook. It reads the hook event on standard input, checks the file the agent just edited, and
/// hands the findings back to the agent: on standard error with exit code 2, which is how an agent hook says "fix this".
/// A hook gives fast feedback only. If the gate itself fails here the edit is not held up; CI is the gate.
/// </summary>
internal static class HookCommand
{
    public const int Feedback = 2;
    public const int NotBlocking = 1;

    public static int Run()
    {
        try
        {
            var (root, file) = Event(Surroundings.ReadInput());
            var result = file.Length == 0 ? new FileResult(C6Hardening.Passed, []) : FileCheck.Check(root, file);
            return result.ExitCode == C6Hardening.Passed ? C6Hardening.Passed : Show(result);
        }
        catch (GateFailure failure)
        {
            Surroundings.PrintError($"c-lean hook: could not check the edit: {failure.Message}");
            return NotBlocking;
        }
    }

    private static int Show(FileResult result)
    {
        Surroundings.PrintError("c-lean: the file you just edited breaks rules of this repository. Fix the code now, before editing anything else:");
        foreach (var line in result.Lines)
        {
            Surroundings.PrintError(line);
        }

        return Feedback;
    }

    /// <summary>The working directory and the edited file of a hook event: `cwd` and `tool_input.file_path`.</summary>
    private static (string Root, string File) Event(string input)
    {
        try
        {
            using var document = JsonDocument.Parse(input);
            var root = Json.Text(document.RootElement, "cwd");
            var file = Json.Text(Json.Object(document.RootElement, "tool_input"), "file_path");
            return (root.Length > 0 ? root : Options.Here, file);
        }
        catch (JsonException problem)
        {
            throw new GateFailure($"standard input is not a hook event: {problem.Message} `c-lean hook` is run by the agent's hook, which passes the event as JSON.");
        }
    }
}
