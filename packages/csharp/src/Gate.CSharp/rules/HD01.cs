// HD01: An inline suppression must name the rule ID, a reason, an expiry date and an approver
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class HD01
{
    private static readonly string[] SuppressingAttributes = ["SuppressMessage", "UnconditionalSuppressMessage"];

    public static IEnumerable<Finding> Check(Solution solution, IReadOnlyList<Finding> findings, Settings settings)
    {
        var malformed = solution.Files.SelectMany(Suppressions.In)
            .Select(suppression => (Suppression: suppression, Problem: Problem(suppression, findings, settings)))
            .Where(found => found.Problem != "")
            .Select(
                found =>
                    Findings.OnLine("HD01", found.Suppression.File, found.Suppression.Line, Message(found.Problem)));
        var switches = solution.Files.SelectMany(file => ToolSwitches(file)
            .Select(line => Findings.OnLine("HD01", file, line, Message("is a compiler or linter disable switch"))));
        return malformed.Concat(switches);
    }

    private static string Message(string problem) =>
        $"HD01: this suppression {problem}. "
        + "Remove it, or write the accepted form with a rule ID, reason=\"<text>\", expires=<YYYY-MM-DD> and "
        + "approver=<name>.";

    private static string Problem(Suppression suppression, IReadOnlyList<Finding> findings, Settings settings)
    {
        if (!Config.RuleTable.IsKnown(suppression.Rule))
        {
            return $"names no known rule ID (found '{suppression.Rule}')";
        }
        if (suppression.Reason == "" || suppression.Expires == "" || suppression.Approver == "")
        {
            return "is missing a reason, an expiry date or an approver";
        }
        if (!Suppressions.IsDate(suppression.Expires))
        {
            return $"expires on {suppression.Expires}, which is not a day that exists";
        }
        if (string.CompareOrdinal(suppression.Expires, settings.Today) < 0)
        {
            return $"expired on {suppression.Expires}, and today is {settings.Today}";
        }
        return findings.Any(finding => Suppressions.Covers(suppression, finding))
            ? "" : $"silences 0 {suppression.Rule} findings on line {suppression.Covered}, the one line it covers";
    }

    private static IEnumerable<int> ToolSwitches(SourceFile file)
    {
        var root = file.Tree.GetRoot();
        var pragmas = root.DescendantTrivia()
            .Select(trivia => trivia.GetStructure())
            .OfType<PragmaWarningDirectiveTriviaSyntax>()
            .Where(pragma => pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword)).Select(Lines.Of);
        var nullables = root.DescendantTrivia()
            .Select(trivia => trivia.GetStructure())
            .OfType<NullableDirectiveTriviaSyntax>()
            .Where(directive => directive.SettingToken.IsKind(SyntaxKind.DisableKeyword)).Select(Lines.Of);
        var attributes = root.DescendantNodes().OfType<AttributeSyntax>()
            .Where(attribute => IsSuppressing(attribute, file.Model))
            .Select(Lines.Of);
        return pragmas.Concat(nullables).Concat(attributes);
    }

    /// <summary>By the name as written, so that an unresolved attribute counts, and by the attribute class it resolves
    /// to, so that an alias counts.</summary>
    private static bool IsSuppressing(AttributeSyntax attribute, SemanticModel model)
    {
        var resolved = Symbols.Lookup(model, attribute)?.ContainingType?.Name ?? "";
        return SuppressingAttributes.Any(
            name => attribute.Name.ToString().Contains(name) || resolved == $"{name}Attribute");
    }
}
