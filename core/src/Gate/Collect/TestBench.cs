using Gate.Contracts;
using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Where a bench works: the repository, a throwaway checkout of it, and the directory its results go to.</summary>
internal sealed record Bench(string Root, string Tree, string Out);

/// <summary>What may be mutated (no lines: everything) and the analyzed head, from which the tests for each mutant are selected.</summary>
internal sealed record MutationScope(IReadOnlyList<LineRange> Lines, Snapshot Head);

/// <summary>One language's turn on a bench: its test command and what it may mutate.</summary>
internal sealed record Turn(Bench Bench, string Language, string Command, MutationScope Scope);

/// <summary>One run of the test command. A mutant ID switches that mutant on; Built says an earlier run built the checkout.</summary>
internal sealed record TestCall(Selection Tests, double Limit)
{
    public string Mutant { get; init; } = "";

    /// <summary>A file the switched code appends the IDs of the mutants it reaches to.</summary>
    public string Reached { get; init; } = "";

    public string Built { get; init; } = "";
}

internal sealed record TestOutcome(bool TimedOut, string Junit, string Error);

/// <summary>
/// Runs the project's own test command: for the suite, per mutant, and instrumented.
/// No mutation tool, coverage tool or tracing SDK is involved.
/// </summary>
internal static class TestBench
{
    private const double TimeoutFactor = 5;
    private const double TimeoutFloorSeconds = 30;
    private const int TailLines = 15;
    private const string AlreadyBuilt = "1";

    /// <summary>The base of a change: only the suite, for the names and times of the tests that exist.</summary>
    public static void RunBase(Bench bench, Config config)
    {
        foreach (var language in config.Tests.Commands.Keys)
        {
            var turn = new Turn(bench, language, config.Tests.Commands[language], new MutationScope([], Empty));
            Setup(turn, config);
            Files.Write(Path.Combine(bench.Out, language, "tests.xml"), Suite(turn, ""));
        }
    }

    private static readonly Snapshot Empty = new([], new Graph([], []), [])
    {
        HasMutationReport = false,
        Mutants = [],
        Tests = [],
        Spans = [],
    };

    /// <summary>The head of a change, or the whole code for the nightly run: suite, traces and the kill matrix.</summary>
    public static void RunHead(Bench bench, Config config, MutationScope scope)
    {
        foreach (var language in config.Tests.Commands.Keys)
        {
            var turn = new Turn(bench, language, config.Tests.Commands[language], scope);
            Setup(turn, config);
            var sheet = Sheet(turn);
            var results = sheet.Switched ? Switched(turn, sheet.Plans) : Rewritten(turn, sheet.Plans);
            var tests = Documents.Read(Path.Combine(bench.Out, language, "tests.xml"), InterchangeText.Tests);
            Files.Write(Path.Combine(bench.Out, language, "mutation.json"), Mutation.Report(results, tests.Select(test => test.Name).ToList()));
            Surroundings.Print($"c-lean: {language}: {results.Count} mutants, {results.Count(result => result.Status == Mutation.Survived)} survived{(sheet.Switched ? ", switched in one build" : "")}");
        }
    }

    /// <summary>
    /// The mutants are compiled into the checkout behind a switch. One instrumented build serves the suite, the traces and
    /// the list of mutants any test reaches; then one test run per reached mutant, in parallel, without rebuilding.
    /// </summary>
    private static IReadOnlyList<MutantResult> Switched(Turn turn, IReadOnlyList<MutantPlan> plans)
    {
        var edges = Instrument(turn);
        var reachedFile = Files.Absolute(Path.Combine(turn.Bench.Out, turn.Language, "reached.txt"));
        Files.Write(reachedFile, "");
        var suite = SuiteTests(turn, reachedFile);
        Files.Write(Path.Combine(turn.Bench.Out, turn.Language, "traces.json"), Traces.Otlp(Traces.Edges(Files.Read(edges))));
        var reached = Files.Read(reachedFile).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
        var limit = Limit(suite);
        return Runnable(suite, plans)
            .AsParallel().AsOrdered().WithDegreeOfParallelism(Surroundings.Processors())
            .Select(plan => Known(plan, reached) ?? Result(plan, RunTests(turn, new TestCall(For(turn, plan), limit) { Mutant = plan.Id, Built = AlreadyBuilt })))
            .ToList();
    }

    /// <summary>The outcome of a mutant that needs no run: the package backed it out, or no test reaches it.</summary>
    private static MutantResult? Known(MutantPlan plan, IReadOnlySet<string> reached) =>
        plan.Status.Length > 0 ? new MutantResult(plan, plan.Status, [])
        : reached.Count > 0 && !reached.Contains(plan.Id) ? new MutantResult(plan, Mutation.NoCoverage, [])
        : null;

    /// <summary>Each mutant is applied by replacing bytes of its file, one at a time; then the code is instrumented and the suite run once more.</summary>
    private static IReadOnlyList<MutantResult> Rewritten(Turn turn, IReadOnlyList<MutantPlan> plans)
    {
        var suite = SuiteTests(turn, "");
        var limit = Limit(suite);
        var results = Runnable(suite, plans).Select(plan => Try(turn, plan, limit)).ToList();
        var edges = Instrument(turn);
        RunTests(turn, new TestCall(TestSelection.Everything, 0));
        Files.Write(Path.Combine(turn.Bench.Out, turn.Language, "traces.json"), Traces.Otlp(Traces.Edges(Files.Read(edges))));
        return results;
    }

    /// <summary>The tests that reach the mutated function.</summary>
    private static Selection For(Turn turn, MutantPlan plan) =>
        TestSelection.Reaching(turn.Scope.Head, [new LineRange(plan.File, plan.Line, plan.Line)]);

    /// <summary>While the suite is red every mutant would look killed, so none is run.</summary>
    private static IReadOnlyList<MutantPlan> Runnable(IReadOnlyList<TestRun> suite, IReadOnlyList<MutantPlan> plans) =>
        suite.Any(test => test.Failed) ? [] : plans;

    private static double Limit(IReadOnlyList<TestRun> suite) =>
        (suite.Sum(test => test.Seconds) * TimeoutFactor) + TimeoutFloorSeconds;

    private static IReadOnlyList<TestRun> SuiteTests(Turn turn, string reachedFile)
    {
        var path = Path.Combine(turn.Bench.Out, turn.Language, "tests.xml");
        Files.Write(path, Suite(turn, reachedFile));
        return Documents.Read(path, InterchangeText.Tests);
    }

    private static void Setup(Turn turn, Config config)
    {
        var command = config.Tests.Setup.GetValueOrDefault(turn.Language, "");
        var result = command.Length > 0 ? Processes.Run(new ProcessCall("sh", ["-c", command], turn.Bench.Tree)) : new ProcessResult(0, "", "");
        if (result.ExitCode != 0)
        {
            throw new GateFailure($"gate.yaml: `tests.setup.{turn.Language}` (`{command}`) failed with exit code {result.ExitCode} in a fresh checkout:\n      {Tail(result.Error + result.Output)}");
        }
    }

    /// <summary>The suite's JUnit XML. A command that leaves none cannot be used: the gate fails rather than pass.</summary>
    private static string Suite(Turn turn, string reachedFile)
    {
        var outcome = RunTests(turn, new TestCall(TestSelection.Everything, 0) { Reached = reachedFile });
        return outcome.Junit.Length > 0
            ? outcome.Junit
            : throw new GateFailure($"gate.yaml: the test command `{turn.Command}` wrote no JUnit XML or TRX to the path in $GATE_JUNIT. It must run the tests and write their results there. Its output:\n      {Tail(outcome.Error)}");
    }

    /// <summary>Asks the package for the mutants; with `--switch` a package that can also compiles them into the checkout.</summary>
    private static MutantSheet Sheet(Turn turn)
    {
        var bench = turn.Bench;
        var lines = Files.Absolute(Path.Combine(bench.Out, turn.Language, "lines.yaml"));
        Files.Write(lines, "lines:\n" + string.Concat(turn.Scope.Lines.Select(range => $"  - {{path: \"{range.Path}\", start: {range.Start}, end: {range.End}}}\n")));
        var only = turn.Scope.Lines.Count > 0 ? new[] { "--lines", lines } : [];
        var arguments = new[] { "mutants", "--switch", "--root", bench.Tree, "--out", Files.Absolute(Path.Combine(bench.Out, turn.Language)) };
        NativeRunner.Run(new Analysis(bench.Root, bench.Tree, bench.Out), turn.Language, [.. arguments, .. Protected(bench.Root), .. only]);
        return Documents.Read(Path.Combine(bench.Out, turn.Language, "mutants.json"), MutationText.Sheet);
    }

    /// <summary>Instruments the checkout for this language and answers the file the observed edges are appended to.</summary>
    private static string Instrument(Turn turn)
    {
        var bench = turn.Bench;
        var edges = Files.Absolute(Path.Combine(bench.Out, turn.Language, Layout.TraceEdges));
        Files.Write(edges, "");
        NativeRunner.Run(new Analysis(bench.Root, bench.Tree, bench.Out), turn.Language, ["instrument", "--root", bench.Tree, .. Protected(bench.Root), "--trace-file", edges]);
        return edges;
    }

    private static MutantResult Try(Turn turn, MutantPlan plan, double limit)
    {
        var path = Path.Combine(turn.Bench.Tree, plan.File);
        var original = Files.ReadBytes(path);
        Files.WriteBytes(path, Mutation.Apply(original, plan));
        try
        {
            return Result(plan, RunTests(turn, new TestCall(For(turn, plan), limit)));
        }
        finally
        {
            Files.WriteBytes(path, original);
        }
    }

    private static MutantResult Result(MutantPlan plan, TestOutcome outcome)
    {
        var tests = Readable(outcome.Junit);
        var killers = tests.Where(test => test.Failed).Select(test => test.Name).ToList();
        return new MutantResult(plan, outcome.TimedOut ? Mutation.Timeout : Mutation.Status(tests), killers);
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

    private static TestOutcome RunTests(Turn turn, TestCall call)
    {
        var junit = Surroundings.TemporaryDirectory() + ".xml";
        var variables = new Dictionary<string, string>
        {
            ["GATE_JUNIT"] = junit,
            ["GATE_TEST_FILES"] = string.Join(' ', call.Tests.Files),
            ["GATE_TESTS"] = string.Join('|', call.Tests.Names),
            ["GATE_MUTANT"] = call.Mutant,
            ["GATE_MUTANT_REACHED"] = call.Reached,
            ["GATE_BUILT"] = call.Built,
            ["PYTHONDONTWRITEBYTECODE"] = "1",
        };
        var result = Processes.Run(new ProcessCall("sh", ["-c", turn.Command], turn.Bench.Tree) { Variables = variables, TimeoutSeconds = call.Limit });
        var text = Files.Read(junit);
        Files.Remove(junit);
        return new TestOutcome(result.ExitCode == Processes.TimedOut, text, result.Error + result.Output);
    }

    private static IReadOnlyList<string> Protected(string root) =>
        ["--config", Files.Absolute(Path.Combine(root, Layout.Config)), "--architecture", Files.Absolute(Path.Combine(root, Layout.Architecture))];

    private static string Tail(string text) =>
        string.Join("\n      ", text.Trim().Split('\n').TakeLast(TailLines));
}
