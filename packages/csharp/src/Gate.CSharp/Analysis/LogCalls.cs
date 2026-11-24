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

    public static bool IsLogCall(InvocationExpressionSyntax call) => HasWord(call.Expression.ToString());

    /// <summary>A log call as a statement writes it: also awaited, discarded with `_ =`, or called with `?.` on a
    /// logger.</summary>
    public static bool IsLog(ExpressionSyntax expression) => expression switch
    {
        InvocationExpressionSyntax call => IsLogCall(call),
        AwaitExpressionSyntax awaited => IsLog(awaited.Expression),
        ParenthesizedExpressionSyntax inner => IsLog(inner.Expression),
        ConditionalAccessExpressionSyntax access => HasWord(access.Expression.ToString()) || IsLog(access.WhenNotNull),
        AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier.ValueText: "_" } } discard =>
            IsLog(discard.Right),
        _ => false,
    };

    private static bool HasWord(string callee) =>
        Words.Any(word => callee.Contains(word, StringComparison.OrdinalIgnoreCase));
}
