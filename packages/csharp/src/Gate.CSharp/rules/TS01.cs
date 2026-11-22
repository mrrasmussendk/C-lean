// TS01: A test must contain at least one assertion
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class TS01
{
    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var assertions = TestCode.Assertions(unit.Body).Count() + HelperAssertions(unit);
        if (!TestCode.IsTest(unit.Node) || assertions > 0)
        {
            return [];
        }
        var message = $"TS01: test {unit.Name} contains 0 assertions, minimum 1. "
            + "Assert on the observable result of the code under test.";
        return [Findings.OnFunction("TS01", unit, message)];
    }

    private static int HelperAssertions(FunctionUnit unit) =>
        unit.Body.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => Symbols.Lookup(unit.File.Model, call)).OfType<IMethodSymbol>()
            .SelectMany(helper => helper.DeclaringSyntaxReferences)
            .Where(reference => reference.SyntaxTree == unit.File.Tree)
            .Sum(reference => TestCode.Assertions(reference.GetSyntax()).Count());
}
