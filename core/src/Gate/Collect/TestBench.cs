using Gate.Contracts;
using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Where a bench works: the repository, a throwaway checkout of it, and the directory its results go to.</summary>
internal sealed record Bench(string Root, string Tree, string Out);

internal sealed record MutationScope(IReadOnlyList<LineRange> Lines, IReadOnlyList<string> TestFiles);

internal sealed record TestOutcome(bool TimedOut, string Junit, string Error);

/// <summary>
/// Runs the project's own test command: once for the suite, once per mutant, once instrumented.
/// No mutation tool, coverage tool or tracing SDK is involved.
/// </summary>
internal static class TestBench
{
    private const double TimeoutFactor = 5;
    private const double TimeoutFloorSeconds = 30;

    public static void RunSuite(Bench bench, Config config)
    {
        foreach (var language in config.Tests.Commands.Keys)
        {
            Setup(bench, config, language);
            var outcome = RunTests(bench, config.Tests.Commands[language], new MutationScope([], []), 0);
            Files.Write(Path.Combine(bench.Out, language, "tests.xml"), Suite(config.Tests.Commands[language], outcome));
        }
    }

    public static void RunMutants(Bench bench, Config config, MutationScope scope)
    {
        foreach (var language in config.Tests.Commands.Keys)
        {
            var suite = Documents.Read(Path.Combine(bench.Out, language, "tests.xml"), InterchangeText.Tests);
            var limit = (suite.Sum(test => test.Seconds) * TimeoutFactor) + TimeoutFloorSeconds;
            var plans = suite.Any(test => test.Failed) ? [] : Plans(bench, language, scope);
            var results = plans.Select(plan => Try(bench, (config.Tests.Commands[language], scope, limit), plan)).ToList();
            var report = Mutation.Report(results, suite.Select(test => test.Name).ToList());
            Files.Write(Path.Combine(bench.Out, language, "mutation.json"), report);
            Surroundings.Print($"gate: {language}: {results.Count} mutants, {results.Count(result => result.Status == Mutation.Survived)} survived");
        }
    }

    /// <summary>Instruments the checkout, runs the suite again, and writes the component edges it observed.</summary>
    public static void RunTraced(Bench bench, Config config)
    {
        var analysis = new Analysis(bench.Root, bench.Tree, bench.Out);
        foreach (var language in config.Languages)
        {
            var edges = Files.Absolute(Path.Combine(bench.Out, language, Layout.TraceEdges));
            Files.Write(edges, "");
            NativeRunner.Run(analysis, language, ["instrument", "--root", bench.Tree, .. Protected(bench.Root), "--trace-file", edges]);
        }

        foreach (var command in config.Tests.Commands.Values)
        {
            RunTests(bench, command, new MutationScope([], []), 0);
        }

        foreach (var language in config.Languages)
        {
            var edges = Traces.Edges(Files.Read(Path.Combine(bench.Out, language, Layout.TraceEdges)));
            Files.Write(Path.Combine(bench.Out, language, "traces.json"), Traces.Otlp(edges));
        }
    }

    private static void Setup(Bench bench, Config config, string language)
    {
        var command = config.Tests.Setup.GetValueOrDefault(language, "");
        var result = command.Length > 0 ? Processes.Run(new ProcessCall("sh", ["-c", command], bench.Tree)) : new ProcessResult(0, "", "");
        if (result.ExitCode != 0)
        {
            throw new GateFailure($"gate.yaml: `tests.setup.{language}` (`{command}`) failed with exit code {result.ExitCode} in a fresh checkout:\n      {Tail(result.Error + result.Output)}");
        }
    }

    /// <summary>The suite's JUnit XML. A command that leaves none cannot be used: the gate fails rather than pass.</summary>
    private static string Suite(string command, TestOutcome outcome) =>
        outcome.Junit.Length > 0
            ? outcome.Junit
            : throw new GateFailure($"gate.yaml: the test command `{command}` wrote no JUnit XML to the path in $GATE_JUNIT. It must run the tests and write their results there. Its output:\n      {Tail(outcome.Error)}");

    private static IReadOnlyList<MutantPlan> Plans(Bench bench, string language, MutationScope scope)
    {
        var lines = Files.Absolute(Path.Combine(bench.Out, language, "lines.yaml"));
        Files.Write(lines, "lines:\n" + string.Concat(scope.Lines.Select(range => $"  - {{path: \"{range.Path}\", start: {range.Start}, end: {range.End}}}\n")));
        var only = scope.Lines.Count > 0 ? new[] { "--lines", lines } : [];
        var analysis = new Analysis(bench.Root, bench.Tree, bench.Out);
        NativeRunner.Run(analysis, language, ["mutants", "--root", bench.Tree, "--out", Files.Absolute(Path.Combine(bench.Out, language)), .. Protected(bench.Root), .. only]);
        return Documents.Read(Path.Combine(bench.Out, language, "mutants.json"), MutationText.Plans);
    }

    private static MutantResult Try(Bench bench, (string Command, MutationScope Scope, double Limit) run, MutantPlan plan)
    {
        var path = Path.Combine(bench.Tree, plan.File);
        var original = Files.ReadBytes(path);
        Files.WriteBytes(path, Mutation.Apply(original, plan));
        try
        {
            var outcome = RunTests(bench, run.Command, run.Scope, run.Limit);
            var tests = Readable(outcome.Junit);
            var killers = tests.Where(test => test.Failed).Select(test => test.Name).ToList();
            return new MutantResult(plan, outcome.TimedOut ? Mutation.Timeout : Mutation.Status(tests), killers);
        }
        finally
        {
            Files.WriteBytes(path, original);
        }
    }

    /// <summary>The tests of a mutant's run. A mutant can break the run so badly that it leaves half a file; that is no test.</summary>
    private static IReadOnlyList<TestRun> Readable(string junit)
    {
        try
        {
            return InterchangeText.Tests(junit);
        }
        catch (System.Xml.XmlException)
        {
            return [];
        }
    }

    private static TestOutcome RunTests(Bench bench, string command, MutationScope scope, double limit)
    {
        var junit = Surroundings.TemporaryDirectory() + ".xml";
        var variables = new Dictionary<string, string>
        {
            ["GATE_JUNIT"] = junit,
            ["GATE_TEST_FILES"] = string.Join(' ', scope.TestFiles),
            ["PYTHONDONTWRITEBYTECODE"] = "1",
        };
        var result = Processes.Run(new ProcessCall("sh", ["-c", command], bench.Tree) { Variables = variables, TimeoutSeconds = limit });
        var text = Files.Read(junit);
        Files.Remove(junit);
        return new TestOutcome(result.ExitCode == Processes.TimedOut, text, result.Error + result.Output);
    }

    private static IReadOnlyList<string> Protected(string root) =>
        ["--config", Files.Absolute(Path.Combine(root, Layout.Config)), "--architecture", Files.Absolute(Path.Combine(root, Layout.Architecture))];

    private static string Tail(string text) =>
        string.Join("\n      ", text.Trim().Split('\n').TakeLast(TailLines));

    private const int TailLines = 15;
}
