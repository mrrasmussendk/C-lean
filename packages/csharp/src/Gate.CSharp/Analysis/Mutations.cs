using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

internal sealed record Mutation(SyntaxNode Site, IParameterSymbol Target);

/// <summary>
/// Writes into an object received as a parameter: member or element assignment, or a
/// standard-library collection mutator called on it. Rebinding the parameter is not a mutation.
/// </summary>
internal static class Mutations
{
    private static readonly string[] Mutators =
    [
        "Add", "AddRange", "AddFirst", "AddLast", "Insert", "InsertRange", "Remove", "RemoveAt", "RemoveAll",
        "RemoveRange", "RemoveFirst", "RemoveLast", "RemoveWhere", "Clear", "Push", "Pop", "Enqueue", "Dequeue",
        "Sort", "Reverse", "TryAdd", "TryRemove", "UnionWith", "ExceptWith", "IntersectWith", "SymmetricExceptWith",
        "TrimExcess", "Fill",
    ];

    private static readonly string[] CollectionNamespaces =
    [
        "System.Collections", "System.Collections.Generic", "System.Collections.Concurrent",
        "System.Collections.ObjectModel", "System.Collections.Specialized",
    ];

    public static IEnumerable<Mutation> In(SyntaxNode scope, SemanticModel model) =>
        scope.DescendantNodesAndSelf().SelectMany(node => Of(node, model));

    private static IEnumerable<Mutation> Of(SyntaxNode node, SemanticModel model)
    {
        var holder = Holder(Writes.Target(node)) ?? MutatorReceiver(node, model);
        var parameter = holder is null ? null : Symbols.Lookup(model, Root(holder)) as IParameterSymbol;
        return parameter is not null && IsCallerVisible(parameter) ? [new Mutation(node, parameter)] : [];
    }

    private static ExpressionSyntax? Holder(ExpressionSyntax? target) => target switch
    {
        MemberAccessExpressionSyntax member => member.Expression,
        ElementAccessExpressionSyntax element => element.Expression,
        _ => null,
    };

    private static ExpressionSyntax? MutatorReceiver(SyntaxNode node, SemanticModel model) =>
        node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } && IsMutator(member, model)
            ? member.Expression
            : null;

    private static bool IsMutator(MemberAccessExpressionSyntax member, SemanticModel model)
    {
        var home = Symbols.Lookup(model, member)?.ContainingNamespace?.ToDisplayString() ?? "";
        return Mutators.Contains(member.Name.Identifier.ValueText) && CollectionNamespaces.Contains(home);
    }

    private static ExpressionSyntax Root(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax member => Root(member.Expression),
        ElementAccessExpressionSyntax element => Root(element.Expression),
        ParenthesizedExpressionSyntax inner => Root(inner.Expression),
        PostfixUnaryExpressionSyntax forgiven => Root(forgiven.Operand),
        _ => expression,
    };

    private static bool IsCallerVisible(IParameterSymbol parameter) =>
        !parameter.Type.IsValueType && !Symbols.IsReceiver(parameter);
}
