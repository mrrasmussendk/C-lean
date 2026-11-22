using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>An assertion call and the expressions it asserts on (none for an interaction check).</summary>
internal sealed record Assertion(InvocationExpressionSyntax Call, IReadOnlyList<ExpressionSyntax> Actuals);

/// <summary>Recognises xUnit, NUnit and MSTest tests and assertions by name, so unresolved packages do not
/// matter.</summary>
internal static class TestCode
{
    private const string AttributeSuffix = "Attribute";

    private static readonly string[] TestAttributes =
        ["Fact", "Theory", "Test", "TestCase", "TestCaseSource", "TestMethod", "DataTestMethod"];

    private static readonly string[] PropertyCalls = ["Prop.ForAll", "Check.Quick", "Check.QuickThrowOnFailure"];

    private static readonly string[] AssertClasses = ["Assert", "CollectionAssert", "StringAssert", "ClassicAssert"];

    private static readonly string[] Interactions =
    [
        "Verify", "VerifyAll", "VerifyGet", "VerifySet", "VerifyNoOtherCalls",
        "Received", "DidNotReceive", "ReceivedWithAnyArgs", "DidNotReceiveWithAnyArgs",
    ];

    public static bool IsTest(SyntaxNode function) =>
        Attributes(function).Any(TestAttributes.Contains);

    /// <summary>FsCheck: a [Property] attribute, or a call to Prop.ForAll or Check.Quick.</summary>
    public static bool IsPropertyBased(SyntaxNode function) =>
        Attributes(function).Contains("Property")
        || function.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(access => PropertyCalls.Contains(access.ToString().Split('<').First()));

    public static IEnumerable<Assertion> Assertions(SyntaxNode scope) =>
        scope.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().SelectMany(Recognise);

    private static IEnumerable<string> Attributes(SyntaxNode function) =>
        function is MethodDeclarationSyntax method
            ? method.AttributeLists.SelectMany(list => list.Attributes).Select(Name) : [];

    private static string Name(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString().Split('.').Last();
        return name.EndsWith(AttributeSuffix) ? name[..^AttributeSuffix.Length] : name;
    }

    private static IEnumerable<Assertion> Recognise(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax owner } when AssertClasses.Contains(
            owner.Identifier.ValueText)
            => [new Assertion(call, call.ArgumentList.Arguments.Select(argument => argument.Expression).ToList())],
        MemberAccessExpressionSyntax member when member.Name.Identifier.ValueText == "Should"
            => [new Assertion(call, [member.Expression])],
        MemberAccessExpressionSyntax member when Interactions.Contains(member.Name.Identifier.ValueText)
            => [new Assertion(call, [])],
        _ => [],
    };
}
