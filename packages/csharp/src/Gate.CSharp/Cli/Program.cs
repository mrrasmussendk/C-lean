using System;
using System.Linq;
using System.Threading;
using Gate.CSharp.Analysis;
using Gate.CSharp.Effects;
using Gate.CSharp.Model;
using Gate.CSharp.Pipeline;

namespace Gate.CSharp.Cli;

/// <summary>gate-csharp: exit 0 when the command ran, 2 when the package failed or its inputs are malformed.</summary>
internal static class Program
{
    private const int Failed = 2;

    /// <summary>
    /// The stack of the thread the command runs on, 256 MB. The rules that measure a function walk it without
    /// recursion; this is for the compiler and the remaining walks, so that source nested tens of thousands deep
    /// is analyzed, not answered with a stack overflow.
    /// </summary>
    private const int StackBytes = 268435456;

    private static int Main(string[] arguments)
    {
        if (arguments.SequenceEqual(["--version"]))
        {
            ConsoleOutput.Version();
            return 0;
        }
        var code = Failed;
        var worker = new Thread(() => code = Guarded(arguments), StackBytes);
        worker.Start();
        worker.Join();
        return code;
    }

    /// <summary>The command with every failure turned into a readable line.</summary>
    private static int Guarded(string[] arguments)
    {
        try
        {
            return Run(Arguments.Parse(arguments));
        }
        catch (GateFailure failure)
        {
            ConsoleOutput.Problems([failure.Message]);
            return Failed;
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException)
        {
            ConsoleOutput.Problems(
                [Failures.Line($"cannot read or write a file: {error.Message.TrimEnd('.')}",
                    "Check the path and its permissions")]);
            return Failed;
        }
        catch (Exception error)
        {
            ConsoleOutput.Problems(
                [Failures.Line($"internal error: {error.Message.TrimEnd('.')}",
                    "Report it with the command line you ran")]);
            return Failed;
        }
    }

    private static int Run(Options options)
    {
        var inputs = SourceReader.Read(options);
        var report = Commands.All[options.Command](inputs, ReferenceReader.Load(inputs));
        OutputWriter.Write(report);
        if (report.Problems.Count == 0)
        {
            return 0;
        }
        ConsoleOutput.Problems(report.Problems);
        return Failed;
    }
}
