using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>The expression a node writes to: the left side of an assignment or the operand of ++ and --.</summary>
internal static class Writes
{
    public static ExpressionSyntax? Target(SyntaxNode node) => node switch
    {
        AssignmentExpressionSyntax { Parent: not InitializerExpressionSyntax } assignment => assignment.Left,
        PrefixUnaryExpressionSyntax unary when IsStep(unary) => unary.Operand,
        PostfixUnaryExpressionSyntax unary when IsStep(unary) => unary.Operand,
        _ => null,
    };

    private static bool IsStep(SyntaxNode node) =>
        node.IsKind(SyntaxKind.PreIncrementExpression) || node.IsKind(SyntaxKind.PreDecrementExpression)
        || node.IsKind(SyntaxKind.PostIncrementExpression) || node.IsKind(SyntaxKind.PostDecrementExpression);
}
