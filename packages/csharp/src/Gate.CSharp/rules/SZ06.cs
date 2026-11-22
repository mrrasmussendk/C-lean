// SZ06: A source line must not exceed the limit in characters
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;

namespace Gate.CSharp.Rules;

internal static class SZ06
{
    public static IEnumerable<Finding> Check(SourceFile file, Settings settings)
    {
        var limit = settings.Limits["SZ06"];
        return file.Tree.GetText().Lines
            .Where(line => line.Span.Length > limit)
            .Select(line => Findings.OnLine("SZ06", file, line.LineNumber + 1, Message(line.Span.Length, limit)));
    }

    private static string Message(int length, int limit) =>
        $"SZ06: line is {length} characters long, limit {limit}. Break it at an operator or argument, or name a part "
            + $"of the expression.";
}
