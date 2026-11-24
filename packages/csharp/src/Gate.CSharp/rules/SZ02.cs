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
    /// <summary>A node still to be scored, and the nesting level it is scored at.</summary>
    private sealed record Visit(SyntaxNode Node, int Nesting);

    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var complexity = Functions.Parts(unit.Body).Sum(Score) + Recursions(unit);
        var limit = settings.Limits["SZ02"];
        if (complexity <= limit)
        {
            return [];
        }
        var message = $"SZ02: function {unit.Name} has cognitive complexity {complexity}, limit {limit}. "
            + "Flatten nested branches with early returns or move a decision into a named function.";
        return [Findings.OnFunction("SZ02", unit, message)];
    }

    /// <summary>The sum of what each node adds. The walk keeps its own list of nodes to visit, so that an expression
    /// thousands of operators long does not exhaust the call stack.</summary>
    private static int Score(SyntaxNode root)
    {
        var pending = new Stack<Visit>([new Visit(root, 0)]);
        var total = 0;
        while (pending.Count > 0)
        {
            var visit = pending.Pop();
            total += Increment(visit);
            foreach (var child in Children(visit))
            {
                pending.Push(child);
            }
        }
        return total;
    }

    /// <summary>What a node adds by itself. An else adds one; the if chained to it adds nothing more.</summary>
    private static int Increment(Visit visit) => visit.Node switch
    {
        IfStatementSyntax { Parent: ElseClauseSyntax } => 0,
        ElseClauseSyntax or GotoStatementSyntax => 1,
        IfStatementSyntax or ConditionalExpressionSyntax or SwitchStatementSyntax or SwitchExpressionSyntax
            or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax or CommonForEachStatementSyntax
            or CatchClauseSyntax or FromClauseSyntax or WhereClauseSyntax => 1 + visit.Nesting,
        BinaryExpressionSyntax binary => OperatorRun(binary),
        _ => 0,
    };

    private static IEnumerable<Visit> Children(Visit visit) => visit.Node switch
    {
        IfStatementSyntax branch => branch.ChildNodes()
            .Select(child => new Visit(child, visit.Nesting + (child == branch.Statement ? 1 : 0))),
        ElseClauseSyntax clause =>
            [new Visit(clause.Statement, visit.Nesting + (clause.Statement is IfStatementSyntax ? 0 : 1))],
        QueryExpressionSyntax query => Clauses(query, visit.Nesting),
        _ => visit.Node.ChildNodes().Select(child => new Visit(child, visit.Nesting + Deepening(visit.Node))),
    };

    /// <summary>One for a structure whose content is nested one level deeper, else zero.</summary>
    private static int Deepening(SyntaxNode node) => node switch
    {
        ConditionalExpressionSyntax or SwitchStatementSyntax or SwitchExpressionSyntax or WhileStatementSyntax
            or DoStatementSyntax or ForStatementSyntax or CommonForEachStatementSyntax or CatchClauseSyntax
            or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax => 1,
        _ => 0,
    };

    /// <summary>Each from clause counts as a loop and each where clause as an if; a later clause is nested in the
    /// earlier ones.</summary>
    private static IEnumerable<Visit> Clauses(QueryExpressionSyntax query, int nesting)
    {
        var clauses = query.DescendantNodes(
            node => node is QueryExpressionSyntax or QueryBodySyntax or QueryContinuationSyntax)
            .Where(node => node is not (QueryBodySyntax or QueryContinuationSyntax)).ToList();
        return clauses.Select((clause, index) => new Visit(clause, nesting + clauses.Take(index).Count(IsCounted)));
    }

    private static bool IsCounted(SyntaxNode clause) => clause is FromClauseSyntax or WhereClauseSyntax;

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
            .Count(
                target =>
                    target is not null
                        && SymbolEqualityComparer.Default.Equals(target.OriginalDefinition, unit.Symbol));
}
