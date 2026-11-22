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
        var conditions = unit.Body.DescendantNodesAndSelf().Select(Condition).OfType<ExpressionSyntax>().ToList();
        return unit.Symbol.Parameters
            .Where(
                parameter =>
                    parameter.Type.SpecialType == SpecialType.System_Boolean
                        || parameter.Type.TypeKind == TypeKind.Enum)
            .Where(parameter => conditions.Any(condition => Tests(condition, parameter, unit.File.Model)))
            .Select(parameter => Findings.OnSymbol("AR01", unit.File, parameter, Message(parameter, unit)));
    }

    private static string Message(IParameterSymbol parameter, FunctionUnit unit) =>
        $"AR01: parameter {parameter.Name} of {unit.Name} is a flag that selects a branch. "
        + "Split the function into one function per case, or pass in the behaviour that differs.";

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

    private static bool Tests(ExpressionSyntax condition, IParameterSymbol parameter, SemanticModel model) =>
        condition switch
    {
        ParenthesizedExpressionSyntax inner => Tests(inner.Expression, parameter, model),
        PrefixUnaryExpressionSyntax negation => Tests(negation.Operand, parameter, model),
        IsPatternExpressionSyntax match => Tests(match.Expression, parameter, model),
        BinaryExpressionSyntax binary when IsLogical(binary)
            => Tests(binary.Left, parameter, model) || Tests(binary.Right, parameter, model),
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
