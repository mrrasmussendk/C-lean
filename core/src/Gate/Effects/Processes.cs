using System.Diagnostics;

namespace Gate.Effects;

internal sealed record ProcessCall(string Command, IReadOnlyList<string> Arguments, string Directory)
{
    public string Input { get; init; } = "";
}

internal sealed record ProcessResult(int ExitCode, string Output, string Error);

/// <summary>Starts git, the native packages and the judge command.</summary>
internal static class Processes
{
    private const int CannotStart = 127;

    /// <summary>A command that cannot be started answers like a shell would: exit code 127.</summary>
    public static ProcessResult Run(ProcessCall call)
    {
        try
        {
            return Start(call);
        }
        catch (System.ComponentModel.Win32Exception missing)
        {
            return new ProcessResult(CannotStart, "", $"cannot start {call.Command}: {missing.Message}");
        }
    }

    private static ProcessResult Start(ProcessCall call)
    {
        var start = new ProcessStartInfo(call.Command)
        {
            WorkingDirectory = call.Directory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in call.Arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new GateFailure($"cannot start {call.Command}");
        process.StandardInput.Write(call.Input);
        process.StandardInput.Close();
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output, error.Result);
    }
}
