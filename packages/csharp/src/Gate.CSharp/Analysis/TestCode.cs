using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    /// <summary>The assertion classes of xUnit, NUnit and MSTest, and Shouldly's `Should.Throw`.</summary>
    private static readonly string[] AssertClasses =
        ["Assert", "CollectionAssert", "StringAssert", "ClassicAssert", "Should"];

    /// <summary>FluentAssertions' `.Should()` and Shouldly's `.ShouldBe(...)` family start with this.</summary>
    private const string ShouldPrefix = "Should";

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

    private static IReadOnlyList<ExpressionSyntax> Arguments(InvocationExpressionSyntax call) =>
        call.ArgumentList.Arguments.Select(argument => argument.Expression).ToList();

    /// <summary>The last name of `Assert`, `Xunit.Assert` or `global::Xunit.Assert`; empty for anything else.</summary>
    private static string LastName(SyntaxNode owner) => owner switch
    {
        IdentifierNameSyntax name => name.Identifier.ValueText,
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax aliased => aliased.Name.Identifier.ValueText,
        _ => "",
    };

    /// <summary>A call by bare name in a file that has `using static` of an assertion class, when the file declares
    /// no function of that name: `Equal(3, total)`.</summary>
    private static bool IsStaticAssert(InvocationExpressionSyntax call)
    {
        var root = call.SyntaxTree.GetRoot();
        var name = LastName(call.Expression);
        var imports = root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<UsingDirectiveSyntax>()
            .Any(import => import.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && import.Name is not null
                && AssertClasses.Contains(LastName(import.Name)));
        return imports && !root.DescendantNodes().Any(
            node => node is MethodDeclarationSyntax method && method.Identifier.ValueText == name
                || node is LocalFunctionStatementSyntax local && local.Identifier.ValueText == name);
    }

    private static IEnumerable<Assertion> Recognise(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax member when AssertClasses.Contains(LastName(member.Expression))
            => [new Assertion(call, Arguments(call))],
        MemberAccessExpressionSyntax member when member.Name.Identifier.ValueText.StartsWith(ShouldPrefix)
            => [new Assertion(call, [member.Expression])],
        IdentifierNameSyntax when IsStaticAssert(call) => [new Assertion(call, Arguments(call))],
        MemberAccessExpressionSyntax member when Interactions.Contains(member.Name.Identifier.ValueText)
            => [new Assertion(call, [])],
        _ => [],
    };
}
