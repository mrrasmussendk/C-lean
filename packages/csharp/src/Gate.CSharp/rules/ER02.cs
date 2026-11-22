// ER02: Null must not be returned or passed where the type system can express absence. The language's strict null checking is enabled with its warnings as errors
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class ER02
{
    public static IEnumerable<Finding> Check(Solution solution, Settings settings)
    {
        var returned = solution.Units.SelectMany(ReturnedNulls);
        var inFiles = solution.Files.SelectMany(file => PassedNulls(file).Concat(ForcedNulls(file)));
        var lenient = solution.Projects.Where(project => !project.IsStrict && !project.IsTest).Select(Lenient);
        return returned.Concat(inFiles).Concat(lenient);
    }

    private static IEnumerable<Finding> ReturnedNulls(FunctionUnit unit)
    {
        var declared = Returns.DeclaredType(unit.Symbol);
        var isDeclared = unit.Node is not AnonymousFunctionExpressionSyntax && !unit.File.IsTest && IsNonNull(declared, unit.Symbol.ReturnNullableAnnotation);
        var message = $"ER02: function {unit.Name} returns null but its return type {declared.Name} does not admit null. "
            + "Declare the type nullable, or return an empty value or an option type.";
        return Returns.Of(unit).SelectMany(NullLiterals).Where(_ => isDeclared)
            .Select(literal => Findings.OnLine("ER02", unit.File, Lines.Of(literal), message));
    }

    private static IEnumerable<ExpressionSyntax> NullLiterals(ExpressionSyntax value) => value switch
    {
        LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NullLiteralExpression) => [literal],
        ParenthesizedExpressionSyntax inner => NullLiterals(inner.Expression),
        ConditionalExpressionSyntax choice => NullLiterals(choice.WhenTrue).Concat(NullLiterals(choice.WhenFalse)),
        _ => [],
    };

    private static bool IsNonNull(ITypeSymbol type, NullableAnnotation annotation) =>
        type.IsReferenceType && annotation == NullableAnnotation.NotAnnotated && type.NullableAnnotation != NullableAnnotation.Annotated;

    private static IEnumerable<Finding> PassedNulls(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>()
            .Where(argument => argument.Expression.IsKind(SyntaxKind.NullLiteralExpression))
            .Select(argument => (Site: argument, Target: Parameter(file.Model, argument)))
            .Where(passed => passed.Target is not null && Symbols.IsInSource(passed.Target) && IsNonNull(passed.Target.Type, passed.Target.NullableAnnotation))
            .Select(passed => Findings.OnLine("ER02", file, Lines.Of(passed.Site), PassedMessage(passed.Target)));

    private static IParameterSymbol? Parameter(SemanticModel model, ArgumentSyntax argument) =>
        model.GetOperation(argument) is Microsoft.CodeAnalysis.Operations.IArgumentOperation operation ? operation.Parameter : null;

    private static string PassedMessage(IParameterSymbol? parameter) =>
        $"ER02: null is passed to parameter {parameter?.Name} of {parameter?.ContainingSymbol.Name}, whose type does not admit null. "
        + "Declare the parameter nullable, or pass a value that expresses absence.";

    private static IEnumerable<Finding> ForcedNulls(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes().OfType<PostfixUnaryExpressionSyntax>()
            .Where(forced => forced.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            .Where(forced => forced.Operand.IsKind(SyntaxKind.NullLiteralExpression) || forced.Operand.IsKind(SyntaxKind.DefaultLiteralExpression) || forced.Operand is DefaultExpressionSyntax)
            .Select(forced => Findings.OnLine("ER02", file, Lines.Of(forced), ForcedMessage(forced)));

    private static string ForcedMessage(ExpressionSyntax forced) =>
        $"ER02: {forced} forces null into a type that does not admit it. "
        + "Declare the type nullable, or supply a real value.";

    private static Finding Lenient(ProjectConfig project)
    {
        var message = $"ER02: project {project.Path} does not enable nullable reference types with warnings as errors. "
            + "Set <Nullable>enable</Nullable> and <TreatWarningsAsErrors>true</TreatWarningsAsErrors>.";
        return new Finding("ER02", project.Path, 1, message, $"ER02|{project.Path}|{Hashing.Sha256(project.FirstLine)}", false);
    }
}
