// SZ03: Nesting depth of control structures in a function must not exceed the limit
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class SZ03
{
    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var depth = Depth(unit.Body);
        var limit = settings.Limits["SZ03"];
        if (depth <= limit)
        {
            return [];
        }
        var message = $"SZ03: function {unit.Name} nests control structures {depth} deep, limit {limit}. "
            + "Return early or move the inner block into a named function.";
        return [Findings.OnFunction("SZ03", unit, message)];
    }

    private static int Depth(SyntaxNode node) => node switch
    {
        AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax => 0,
        IfStatementSyntax { Parent: ElseClauseSyntax } => Deepest(node),
        IfStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax
            or CommonForEachStatementSyntax
            or SwitchStatementSyntax or SwitchExpressionSyntax or TryStatementSyntax => 1 + Deepest(node),
        _ => Deepest(node),
    };

    private static int Deepest(SyntaxNode node) => node.ChildNodes().Select(Depth).DefaultIfEmpty(0).Max();
}
