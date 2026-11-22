// SZ02: Cognitive complexity of a function must not exceed the limit
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class SZ02
{
    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var complexity = Score(unit.Body, 0) + Recursions(unit);
        var limit = settings.Limits["SZ02"];
        if (complexity <= limit)
        {
            return [];
        }
        var message = $"SZ02: function {unit.Name} has cognitive complexity {complexity}, limit {limit}. "
            + "Flatten nested branches with early returns or move a decision into a named function.";
        return [Findings.OnFunction("SZ02", unit, message)];
    }

    private static int Score(SyntaxNode node, int nesting) => node switch
    {
        IfStatementSyntax branch => 1 + nesting + Branch(branch, nesting),
        ConditionalExpressionSyntax or SwitchStatementSyntax or SwitchExpressionSyntax or WhileStatementSyntax
            or DoStatementSyntax or ForStatementSyntax or CommonForEachStatementSyntax or CatchClauseSyntax
            => 1 + nesting + Children(node, nesting + 1),
        AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax => Children(node, nesting + 1),
        GotoStatementSyntax => 1,
        BinaryExpressionSyntax binary => OperatorRun(binary) + Children(node, nesting),
        _ => Children(node, nesting),
    };

    private static int Children(SyntaxNode node, int nesting) => node.ChildNodes().Sum(child => Score(child, nesting));

    private static int Branch(IfStatementSyntax branch, int nesting) =>
        Score(branch.Condition, nesting) + Score(branch.Statement, nesting + 1) + Else(branch.Else, nesting);

    private static int Else(ElseClauseSyntax? clause, int nesting) => clause switch
    {
        null => 0,
        { Statement: IfStatementSyntax chained } => 1 + Branch(chained, nesting),
        _ => 1 + Score(clause.Statement, nesting + 1),
    };

    private static int OperatorRun(BinaryExpressionSyntax binary)
    {
        var isLogical = binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression);
        return isLogical && Enclosing(binary)?.RawKind != binary.RawKind ? 1 : 0;
    }

    private static SyntaxNode? Enclosing(SyntaxNode node) =>
        node.Parent is ParenthesizedExpressionSyntax parenthesis ? Enclosing(parenthesis) : node.Parent;

    private static int Recursions(FunctionUnit unit) =>
        unit.Body.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => Symbols.Lookup(unit.File.Model, call))
            .Count(target => target is not null && SymbolEqualityComparer.Default.Equals(target.OriginalDefinition, unit.Symbol));
}
