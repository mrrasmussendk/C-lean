using System;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Effects;
using Gate.CSharp.Model;
using Gate.CSharp.Pipeline;

namespace Gate.CSharp.Cli;

/// <summary>gate-csharp: exit 0 when the analysis ran, 2 when the package failed or its inputs are malformed.</summary>
internal static class Program
{
    private const int Failed = 2;

    private static int Main(string[] arguments)
    {
        if (arguments.SequenceEqual(["--version"]))
        {
            ConsoleOutput.Version();
            return 0;
        }
        try
        {
            return Analyze(Arguments.Parse(arguments));
        }
        catch (GateFailure failure)
        {
            ConsoleOutput.Problems([failure.Message]);
            return Failed;
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException)
        {
            ConsoleOutput.Problems([Failures.Line($"cannot read or write a file: {error.Message.TrimEnd('.')}", "Check the path and its permissions")]);
            return Failed;
        }
        catch (Exception error)
        {
            ConsoleOutput.Problems([Failures.Line($"internal error: {error.Message.TrimEnd('.')}", "Report it with the command line you ran")]);
            return Failed;
        }
    }

    private static int Analyze(Options options)
    {
        var inputs = SourceReader.Read(options);
        var report = Analyzer.Analyze(inputs, ReferenceReader.Load(inputs));
        OutputWriter.Write(options.Out, report);
        if (report.Problems.Count == 0)
        {
            return 0;
        }
        ConsoleOutput.Problems(report.Problems);
        return Failed;
    }
}
