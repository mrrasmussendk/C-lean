// AR01: Flag parameter: a boolean or enum parameter must not be the condition of a branch in the function body
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class AR01
{
    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var tested = unit.Body.DescendantNodesAndSelf().Select(Condition).OfType<ExpressionSyntax>()
            .SelectMany(Operands).ToList();
        return unit.Symbol.Parameters.Concat(Captured(unit))
            .Where(
                parameter =>
                    parameter.Type.SpecialType == SpecialType.System_Boolean
                        || parameter.Type.TypeKind == TypeKind.Enum)
            .Where(parameter => tested.Any(operand => Tests(operand, parameter, unit.File.Model)))
            .Select(parameter => Findings.OnSymbol("AR01", unit.File, parameter, Message(parameter, unit)));
    }

    /// <summary>The parameters of the primary constructor of the function's type, which every member can read. They
    /// are reported under the type's name, so one flag read in several members is one finding.</summary>
    private static IEnumerable<IParameterSymbol> Captured(FunctionUnit unit) =>
        unit.Symbol.ContainingType is { } type && Symbols.PrimaryConstructor(type) is { } constructor
            ? constructor.Parameters.Where(
                parameter => parameter.Locations.Any(location => location.SourceTree == unit.File.Tree))
            : [];

    private static string Message(IParameterSymbol parameter, FunctionUnit unit)
    {
        var owner = Symbols.IsOfPrimaryConstructor(parameter) ? parameter.ContainingType.Name : unit.Name;
        return $"AR01: parameter {parameter.Name} of {owner} is a flag that selects a branch. "
            + "Split the function into one function per case, or pass in the behaviour that differs.";
    }

    private static ExpressionSyntax? Condition(SyntaxNode node) => node switch
    {
        IfStatementSyntax branch => branch.Condition,
        ConditionalExpressionSyntax choice => choice.Condition,
        WhileStatementSyntax loop => loop.Condition,
        DoStatementSyntax loop => loop.Condition,
        ForStatementSyntax loop => loop.Condition,
        SwitchStatementSyntax selection => selection.Expression,
        SwitchExpressionSyntax selection => selection.GoverningExpression,
        _ => null,
    };

    /// <summary>The expressions a condition is made of, with `!`, parentheses, `is` and the logical operators taken
    /// apart. The walk keeps its own list, so a condition of thousands of terms does not exhaust the call
    /// stack.</summary>
    private static IEnumerable<ExpressionSyntax> Operands(ExpressionSyntax condition)
    {
        var pending = new Stack<ExpressionSyntax>([condition]);
        var operands = new List<ExpressionSyntax>();
        while (pending.Count > 0)
        {
            var expression = pending.Pop();
            var parts = Parts(expression).ToList();
            operands.AddRange(parts.Count == 0 ? [expression] : []);
            parts.ForEach(pending.Push);
        }
        return operands;
    }

    private static IEnumerable<ExpressionSyntax> Parts(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax inner => [inner.Expression],
        PrefixUnaryExpressionSyntax negation => [negation.Operand],
        IsPatternExpressionSyntax match => [match.Expression],
        BinaryExpressionSyntax binary when IsLogical(binary) => [binary.Left, binary.Right],
        _ => [],
    };

    private static bool Tests(ExpressionSyntax operand, IParameterSymbol parameter, SemanticModel model) =>
        operand switch
    {
        BinaryExpressionSyntax binary when IsEquality(binary)
            =>
                ComparesWithConstant(binary.Left, binary.Right, parameter, model)
                    || ComparesWithConstant(binary.Right, binary.Left, parameter, model),
        IdentifierNameSyntax name => SymbolEqualityComparer.Default.Equals(Symbols.Lookup(model, name), parameter),
        _ => false,
    };

    private static bool ComparesWithConstant(
        ExpressionSyntax side,
        ExpressionSyntax other,
        IParameterSymbol parameter,
        SemanticModel model) =>
        side is IdentifierNameSyntax && Tests(side, parameter, model) && model.GetConstantValue(other).HasValue;

    private static bool IsLogical(SyntaxNode node) =>
        node.IsKind(SyntaxKind.LogicalAndExpression) || node.IsKind(SyntaxKind.LogicalOrExpression);

    private static bool IsEquality(SyntaxNode node) =>
        node.IsKind(SyntaxKind.EqualsExpression) || node.IsKind(SyntaxKind.NotEqualsExpression);
}
