using Gate.CSharp.Model;

namespace Gate.CSharp.Analysis;

/// <summary>Readable failure lines: where, what is wrong, what to do.</summary>
internal static class Failures
{
    public static string Line(Position position, string problem, string remedy) =>
        $"gate-csharp: {position.File}:{position.Line}:{position.Column}: {problem}. {remedy}.";

    public static string Line(string problem, string remedy) => $"gate-csharp: {problem}. {remedy}.";

    public static GateFailure At(Position position, string problem, string remedy) =>
        new(Line(position, problem, remedy));
}
