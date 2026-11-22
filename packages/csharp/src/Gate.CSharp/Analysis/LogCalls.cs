using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>
/// A call is a log call when its receiver or function name is, or contains, log, logger, logging,
/// console, print, trace or debug, compared without case (the definition ER01 and the mutant context share).
/// </summary>
internal static class LogCalls
{
    private static readonly string[] Words = ["log", "console", "print", "trace", "debug"];

    public static bool IsLogCall(InvocationExpressionSyntax call)
    {
        var callee = call.Expression.ToString();
        return Words.Any(word => callee.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
