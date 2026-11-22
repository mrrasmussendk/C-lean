// TS02: A test must not assert only on values it configured a test double to return
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class TS02
{
    /// <summary>Names in one test: mock holders (Moq), the doubles themselves, and variables assigned from a call on a
    /// double.</summary>
    private sealed record Names(
        IReadOnlyList<string> Holders,
        IReadOnlyList<string> Doubles,
        IReadOnlyList<string> Values);

    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var assertions = TestCode.Assertions(unit.Body).ToList();
        var names = NamesIn(unit.Body);
        var onDoubles = assertions.Count(assertion => assertion.Actuals.Any(actual => IsDoubleValue(actual, names)));
        if (!TestCode.IsTest(unit.Node) || assertions.Count == 0 || onDoubles < assertions.Count)
        {
            return [];
        }
        var message = $"TS02: test {unit.Name} has {assertions.Count} assertions and all {onDoubles} are on values a "
            + $"test double was configured to return. "
            + "Assert on what the code under test returns or does.";
        return [Findings.OnFunction("TS02", unit, message)];
    }

    private static Names NamesIn(SyntaxNode body)
    {
        var declared = body.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Where(variable => variable.Initializer is not null)
            .ToList();
        var holders = Named(
            declared,
            value =>
                value is ObjectCreationExpressionSyntax { Type: GenericNameSyntax { Identifier.ValueText: "Mock" } });
        var created = Named(declared, value => IsFactoryCall(value) || IsObjectOf(value, holders));
        var doubles = new Names(holders, created, []);
        return doubles with { Values = Named(declared, value => IsCallOnDouble(value, doubles)) };
    }

    private static IReadOnlyList<string> Named(
        IEnumerable<VariableDeclaratorSyntax> declared,
        System.Func<ExpressionSyntax,
        bool> matches) =>
        declared.Where(variable => matches(variable.Initializer!.Value))
            .Select(variable => variable.Identifier.ValueText)
            .ToList();

    private static bool IsFactoryCall(ExpressionSyntax value) =>
        value is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax owner }
        }
        && owner.Identifier.ValueText is "Substitute" or "Mock";

    private static bool IsObjectOf(ExpressionSyntax value, IReadOnlyList<string> holders) =>
        value is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Object",
            Expression: IdentifierNameSyntax holder }
        && holders.Contains(holder.Identifier.ValueText);

    private static bool IsDouble(ExpressionSyntax expression, Names names) =>
        IsObjectOf(expression, names.Holders)
        || (expression is IdentifierNameSyntax name && names.Doubles.Contains(name.Identifier.ValueText));

    private static bool IsCallOnDouble(ExpressionSyntax expression, Names names) => expression switch
    {
        InvocationExpressionSyntax call => IsCallOnDouble(call.Expression, names),
        MemberAccessExpressionSyntax member =>
            IsDouble(member.Expression, names) || IsCallOnDouble(member.Expression, names),
        AwaitExpressionSyntax awaited => IsCallOnDouble(awaited.Expression, names),
        ParenthesizedExpressionSyntax inner => IsCallOnDouble(inner.Expression, names),
        _ => false,
    };

    private static bool IsDoubleValue(ExpressionSyntax actual, Names names) =>
        IsCallOnDouble(actual, names)
            || (actual is IdentifierNameSyntax name && names.Values.Contains(name.Identifier.ValueText));
}
