// DC03: A numeric literal other than -1, 0 and 1 must be a named constant in production code
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class DC03
{
    public static IEnumerable<Finding> Check(SourceFile file, Settings settings) =>
        file.Tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
            .Where(literal => literal.IsKind(SyntaxKind.NumericLiteralExpression))
            .Where(literal => !IsAllowedValue(literal) && !IsNamed(Outermost(literal)))
            .Select(literal => Findings.OnLine("DC03", file, Lines.Of(literal), Message(literal)));

    private static string Message(LiteralExpressionSyntax literal) =>
        $"DC03: numeric literal {literal.Token.Text} is not one of -1, 0 and 1 and has no name. "
        + "Declare it as a named constant and use the name.";

    private static bool IsAllowedValue(LiteralExpressionSyntax literal) =>
        System.Convert.ToDouble(literal.Token.Value, System.Globalization.CultureInfo.InvariantCulture) is 0 or 1;

    /// <summary>The literal with its sign and parentheses, so that the whole initializer can be recognised.</summary>
    private static SyntaxNode Outermost(SyntaxNode expression) =>
        expression.Parent is ParenthesizedExpressionSyntax or PrefixUnaryExpressionSyntax ? Outermost(expression.Parent) : expression;

    private static bool IsNamed(SyntaxNode value) => value.Parent switch
    {
        EqualsValueClauseSyntax { Parent: EnumMemberDeclarationSyntax } => true,
        EqualsValueClauseSyntax { Parent.Parent.Parent: FieldDeclarationSyntax field } => IsConstant(field.Modifiers),
        EqualsValueClauseSyntax { Parent.Parent.Parent: LocalDeclarationStatementSyntax local } => IsConstant(local.Modifiers),
        _ => false,
    };

    private static bool IsConstant(SyntaxTokenList modifiers) =>
        modifiers.Any(SyntaxKind.ConstKeyword) || (modifiers.Any(SyntaxKind.StaticKeyword) && modifiers.Any(SyntaxKind.ReadOnlyKeyword));
}
