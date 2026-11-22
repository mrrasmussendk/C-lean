using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Mutants;

/// <summary>
/// Lists the mutants of one file: each is one token replaced by another text. Offsets are bytes
/// into the stored file, so the byte order mark and multi-byte characters before the token count.
/// </summary>
internal static class MutantFinder
{
    private sealed record Swap(string Mutator, string Text);

    private static readonly IReadOnlyDictionary<SyntaxKind, Swap> Swaps = new Dictionary<SyntaxKind, Swap>
    {
        [SyntaxKind.PlusToken] = new("ArithmeticOperator", "-"),
        [SyntaxKind.MinusToken] = new("ArithmeticOperator", "+"),
        [SyntaxKind.AsteriskToken] = new("ArithmeticOperator", "/"),
        [SyntaxKind.SlashToken] = new("ArithmeticOperator", "*"),
        [SyntaxKind.EqualsEqualsToken] = new("EqualityOperator", "!="),
        [SyntaxKind.ExclamationEqualsToken] = new("EqualityOperator", "=="),
        [SyntaxKind.LessThanToken] = new("EqualityOperator", "<="),
        [SyntaxKind.LessThanEqualsToken] = new("EqualityOperator", "<"),
        [SyntaxKind.GreaterThanToken] = new("EqualityOperator", ">="),
        [SyntaxKind.GreaterThanEqualsToken] = new("EqualityOperator", ">"),
        [SyntaxKind.AmpersandAmpersandToken] = new("LogicalOperator", "||"),
        [SyntaxKind.BarBarToken] = new("LogicalOperator", "&&"),
        [SyntaxKind.TrueKeyword] = new("BooleanLiteral", "false"),
        [SyntaxKind.FalseKeyword] = new("BooleanLiteral", "true"),
        [SyntaxKind.ExclamationToken] = new("UnaryOperator", ""),
    };

    public static IEnumerable<Mutant> Of(SourceFile file, int byteOrderMark)
    {
        var text = file.Tree.GetText().ToString();
        return file.Tree.GetRoot().DescendantTokens()
            .Where(token => Swaps.ContainsKey(token.Kind()) && IsMutable(token, file.Model))
            .Select(token => Mutant(file, token, byteOrderMark + Encoding.UTF8.GetByteCount(text.AsSpan(0, token.SpanStart))));
    }

    private static Mutant Mutant(SourceFile file, SyntaxToken token, int start)
    {
        var position = token.GetLocation().GetLineSpan().StartLinePosition;
        var swap = Swaps[token.Kind()];
        return new Mutant(
            $"{file.Path}:{position.Line + 1}:{position.Character + 1}:{swap.Mutator}",
            file.Path,
            position.Line + 1,
            position.Character + 1,
            swap.Mutator,
            start,
            start + Encoding.UTF8.GetByteCount(token.Text),
            token.Text,
            swap.Text,
            Context(token));
    }

    private static bool IsMutable(SyntaxToken token, SemanticModel model)
    {
        var isOperand = token.Parent switch
        {
            BinaryExpressionSyntax binary => !IsStringConcatenation(binary, model),
            LiteralExpressionSyntax => true,
            PrefixUnaryExpressionSyntax unary => unary.IsKind(SyntaxKind.LogicalNotExpression),
            _ => false,
        };
        return isOperand && !token.Parent!.Ancestors().Any(IsExcluded);
    }

    private static bool IsStringConcatenation(BinaryExpressionSyntax binary, SemanticModel model) =>
        binary.IsKind(SyntaxKind.AddExpression) && model.GetTypeInfo(binary).Type?.SpecialType == SpecialType.System_String;

    /// <summary>Attributes, enum member values and constant declarations hold no mutants.</summary>
    private static bool IsExcluded(SyntaxNode node) => node switch
    {
        AttributeListSyntax or EnumMemberDeclarationSyntax => true,
        FieldDeclarationSyntax field => IsConstant(field.Modifiers),
        LocalDeclarationStatementSyntax local => local.Modifiers.Any(SyntaxKind.ConstKeyword),
        _ => false,
    };

    private static bool IsConstant(SyntaxTokenList modifiers) =>
        modifiers.Any(SyntaxKind.ConstKeyword) || (modifiers.Any(SyntaxKind.StaticKeyword) && modifiers.Any(SyntaxKind.ReadOnlyKeyword));

    private static string Context(SyntaxToken token)
    {
        var around = token.Parent!.Ancestors().ToList();
        if (around.OfType<ArgumentListSyntax>().Any(list => list.Parent is InvocationExpressionSyntax call && LogCalls.IsLogCall(call)))
        {
            return "logging";
        }
        var isGuard = around.OfType<IfStatementSyntax>().Any(branch => branch.Condition.Span.Contains(token.Span) && OnlyThrows(branch.Statement));
        return isGuard ? "guard" : "";
    }

    private static bool OnlyThrows(StatementSyntax body) => body switch
    {
        ThrowStatementSyntax => true,
        BlockSyntax block => block.Statements.Count > 0 && block.Statements.All(statement => statement is ThrowStatementSyntax),
        _ => false,
    };
}
