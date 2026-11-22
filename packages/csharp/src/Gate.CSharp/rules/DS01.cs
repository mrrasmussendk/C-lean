// DS01: The same discriminator must not select a branch in more functions than the limit
using System;
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class DS01
{
    /// <summary>One function branching on one discriminator (a named constant, an enum type or a tested
    /// type).</summary>
    private sealed record Use(string Discriminator, FunctionUnit Function);

    private static readonly SyntaxKind[] Comparisons =
    [
        SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression, SyntaxKind.LessThanExpression,
        SyntaxKind.LessThanOrEqualExpression, SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanOrEqualExpression,
    ];

    public static IEnumerable<Finding> Check(Solution solution, Settings settings)
    {
        var limit = settings.Limits["DS01"];
        return solution.Units.SelectMany(Uses).Distinct()
            .GroupBy(use => use.Discriminator)
            .Where(uses => uses.Count() > limit)
            .Select(uses => Finding(uses.Key, Functions.InOrder(uses.Select(use => use.Function)), limit));
    }

    private static Finding Finding(string discriminator, IReadOnlyList<FunctionUnit> functions, int limit)
    {
        var first = functions[0];
        var message = $"DS01: discriminator {discriminator} selects a branch in {functions.Count} functions, limit "
            + $"{limit}: "
            + $"{Functions.Listed(functions)}. Give the cases a type of their own or one function that decides.";
        var key = $"DS01|{first.File.Path}|{first.Id}|{discriminator}";
        return new Finding("DS01", first.File.Path, first.Line, message, key, false);
    }

    private static IEnumerable<Use> Uses(FunctionUnit unit) =>
        Functions.OwnNodes(unit.Body).SelectMany(Deciders)
            .SelectMany(decider => decider.DescendantNodesAndSelf())
            .SelectMany(node => Discriminators(node, unit.File.Model))
            .Select(symbol => new Use(Name(symbol), unit));

    private static string Name(ISymbol symbol) =>
        symbol is ITypeSymbol ? symbol.Name : $"{symbol.ContainingType?.Name}.{symbol.Name}";

    /// <summary>The parts of a branch that decide it: a condition, or the subject and the cases of a switch.</summary>
    private static IEnumerable<SyntaxNode> Deciders(SyntaxNode node) => node switch
    {
        IfStatementSyntax branch => [branch.Condition],
        ConditionalExpressionSyntax choice => [choice.Condition],
        SwitchStatementSyntax selection =>
            selection.Sections.SelectMany(section => section.Labels).Prepend<SyntaxNode>(selection.Expression),
        SwitchExpressionSyntax selection =>
            selection.Arms.Select(arm => arm.Pattern).Prepend<SyntaxNode>(selection.GoverningExpression),
        _ => [],
    };

    private static IEnumerable<ISymbol> Discriminators(SyntaxNode node, SemanticModel model) => node switch
    {
        BinaryExpressionSyntax test when Comparisons.Contains(test.Kind())
            => Named(test.Left, model).Concat(Named(test.Right, model)),
        BinaryExpressionSyntax test when test.IsKind(SyntaxKind.IsExpression) || test.IsKind(SyntaxKind.AsExpression)
            => Named(test.Right, model),
        ConstantPatternSyntax pattern => Named(pattern.Expression, model),
        RelationalPatternSyntax pattern => Named(pattern.Expression, model),
        CaseSwitchLabelSyntax label => Named(label.Value, model),
        DeclarationPatternSyntax pattern => Named(pattern.Type, model),
        TypePatternSyntax pattern => Named(pattern.Type, model),
        RecursivePatternSyntax { Type: { } type } => Named(type, model),
        IdentifierNameSyntax name => CastBefore(name, model).Concat(EnumSubject(name, model)),
        _ => [],
    };

    /// <summary>The constant, enum type or type an expression names; nothing for a literal or a variable.</summary>
    private static IEnumerable<ISymbol> Named(SyntaxNode expression, SemanticModel model) =>
        Symbols.Lookup(model, expression) switch
    {
        IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum } member => [member.ContainingType],
        IFieldSymbol field when field.IsConst || (field.IsStatic && field.IsReadOnly) => [field.OriginalDefinition],
        INamedTypeSymbol type => [type.OriginalDefinition],
        _ => [],
    };

    /// <summary>`var x = value as T;` followed by a test of x is a test for T.</summary>
    private static IEnumerable<ISymbol> CastBefore(IdentifierNameSyntax name, SemanticModel model) =>
        Symbols.Lookup(model, name) is ILocalSymbol local
            ? local.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
                .OfType<VariableDeclaratorSyntax>()
                .Select(declared => declared.Initializer?.Value).OfType<BinaryExpressionSyntax>()
                .Where(value => value.IsKind(SyntaxKind.AsExpression))
                .SelectMany(value => Named(value.Right, model))
            : [];

    /// <summary>The subject of a switch on a value of an enum type is a test of that enum.</summary>
    private static IEnumerable<ISymbol> EnumSubject(IdentifierNameSyntax name, SemanticModel model) =>
        name.Parent is SwitchStatementSyntax or SwitchExpressionSyntax
            && model.GetTypeInfo(name).Type is { TypeKind: TypeKind.Enum } type
            ? [type]
            : [];
}
