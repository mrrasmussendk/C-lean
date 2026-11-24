using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>The expressions a node writes to: the left side of an assignment, each element when that side is a
/// tuple, or the operand of ++ and --.</summary>
internal static class Writes
{
    public static IEnumerable<ExpressionSyntax> Targets(SyntaxNode node) => node switch
    {
        AssignmentExpressionSyntax { Parent: not InitializerExpressionSyntax } assignment => Elements(assignment.Left),
        PrefixUnaryExpressionSyntax unary when IsStep(unary) => [unary.Operand],
        PostfixUnaryExpressionSyntax unary when IsStep(unary) => [unary.Operand],
        _ => [],
    };

    private static IEnumerable<ExpressionSyntax> Elements(ExpressionSyntax target) =>
        target is TupleExpressionSyntax tuple
            ? tuple.Arguments.SelectMany(argument => Elements(argument.Expression))
            : [target];

    private static bool IsStep(SyntaxNode node) =>
        node.IsKind(SyntaxKind.PreIncrementExpression) || node.IsKind(SyntaxKind.PreDecrementExpression)
        || node.IsKind(SyntaxKind.PostIncrementExpression) || node.IsKind(SyntaxKind.PostDecrementExpression);
}
