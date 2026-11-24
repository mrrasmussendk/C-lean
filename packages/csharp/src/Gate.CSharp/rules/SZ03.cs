// SZ03: Nesting depth of control structures in a function must not exceed the limit
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class SZ03
{
    /// <summary>A node still to be measured, and the depth of the control structures around it.</summary>
    private sealed record Level(SyntaxNode Node, int Depth);

    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var depth = Functions.Parts(unit.Body).Select(Depth).DefaultIfEmpty(0).Max();
        var limit = settings.Limits["SZ03"];
        if (depth <= limit)
        {
            return [];
        }
        var message = $"SZ03: function {unit.Name} nests control structures {depth} deep, limit {limit}. "
            + "Return early or move the inner block into a named function.";
        return [Findings.OnFunction("SZ03", unit, message)];
    }

    /// <summary>The deepest nesting under a node. The walk keeps its own list of nodes to visit, so that an
    /// expression thousands of operators long does not exhaust the call stack.</summary>
    private static int Depth(SyntaxNode root)
    {
        var pending = new Stack<Level>([new Level(root, 0)]);
        var deepest = 0;
        while (pending.Count > 0)
        {
            var level = pending.Pop();
            var depth = level.Depth + Step(level.Node);
            deepest = System.Math.Max(deepest, depth);
            foreach (var child in Inner(level.Node))
            {
                pending.Push(new Level(child, depth));
            }
        }
        return deepest;
    }

    private static int Step(SyntaxNode node) => node switch
    {
        IfStatementSyntax { Parent: ElseClauseSyntax } => 0,
        IfStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax
            or CommonForEachStatementSyntax
            or SwitchStatementSyntax or SwitchExpressionSyntax or TryStatementSyntax => 1,
        _ => 0,
    };

    private static IEnumerable<SyntaxNode> Inner(SyntaxNode node) =>
        node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax ? [] : node.ChildNodes();
}
