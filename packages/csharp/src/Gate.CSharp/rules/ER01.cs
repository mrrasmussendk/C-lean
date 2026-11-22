// ER01: A catch block must not be empty and must not only log
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class ER01
{
    public static IEnumerable<Finding> Check(SourceFile file, Settings settings) =>
        file.Tree.GetRoot().DescendantNodes().OfType<CatchClauseSyntax>()
            .Where(clause => clause.Block.Statements.All(IsLogCall))
            .Select(clause => Findings.OnLine("ER01", file, Lines.Of(clause.CatchKeyword), Message(clause)));

    private static string Message(CatchClauseSyntax clause) =>
        $"ER01: catch block has {clause.Block.Statements.Count} statements and none handles the exception. "
        + "Handle it, rethrow it, or remove the catch so the caller sees the failure.";

    private static bool IsLogCall(StatementSyntax statement) =>
        statement is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax call } && LogCalls.IsLogCall(call);
}
