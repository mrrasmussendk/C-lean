using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

internal sealed record StateWrite(SyntaxNode Site, ISymbol Target);

/// <summary>Assignments to instance or static state: a field or property of the receiver, or a static one.</summary>
internal static class StateWrites
{
    public static IEnumerable<StateWrite> In(IEnumerable<SyntaxNode> nodes, SemanticModel model) =>
        nodes.Select(Writes.Target).OfType<ExpressionSyntax>().SelectMany(target => Write(target, model));

    public static bool IsMutableStatic(StateWrite write) =>
        write.Target is IFieldSymbol { IsStatic: true, IsReadOnly: false } or IPropertySymbol { IsStatic: true };

    private static IEnumerable<StateWrite> Write(ExpressionSyntax target, SemanticModel model)
    {
        var symbol = Symbols.Lookup(model, target);
        if (symbol is not (IFieldSymbol or IPropertySymbol))
        {
            return [];
        }
        return symbol.IsStatic || IsOnReceiver(target) ? [new StateWrite(target, symbol)] : [];
    }

    private static bool IsOnReceiver(ExpressionSyntax target) =>
        target is IdentifierNameSyntax
            or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax };
}
