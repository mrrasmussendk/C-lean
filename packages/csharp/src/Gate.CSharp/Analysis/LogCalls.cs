using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>
/// A call is a log call when its receiver or function name has log, logger, logging, console, print, trace or
/// debug as one of its words, compared without case (the definition ER01 and the mutant context share). Words
/// are the parts of an identifier in PascalCase, camelCase or snake_case, so `Logger.LogError` is a log call
/// and `Catalog.Restore` and `ShowErrorDialog` are not.
/// </summary>
internal static class LogCalls
{
    private static readonly string[] Words = ["log", "logger", "logging", "console", "print", "trace", "debug"];

    private static readonly Regex WordForm = new("[A-Z]+(?![a-z])|[A-Z]?[a-z]+");

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
        WordForm.Matches(callee).Any(found => Words.Contains(found.Value, StringComparer.OrdinalIgnoreCase));
}
