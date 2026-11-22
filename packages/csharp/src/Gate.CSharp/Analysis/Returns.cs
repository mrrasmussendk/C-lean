using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>What a function returns: its own return expressions and its declared result type.</summary>
internal static class Returns
{
    public static IEnumerable<ExpressionSyntax> Of(FunctionUnit unit) =>
        unit.Body is ExpressionSyntax expression
            ? [expression]
            : Functions.OwnNodes(unit.Body).OfType<ReturnStatementSyntax>().Select(statement => statement.Expression).OfType<ExpressionSyntax>();

    /// <summary>The declared return type; for an async function, the type inside its task.</summary>
    public static ITypeSymbol DeclaredType(IMethodSymbol method) =>
        method is { IsAsync: true, ReturnType: INamedTypeSymbol { TypeArguments: [var inner] } } ? inner : method.ReturnType;

    public static bool HasValue(IMethodSymbol method) =>
        !method.ReturnsVoid && method.ReturnType is not INamedTypeSymbol { Name: "Task" or "ValueTask", IsGenericType: false };
}
