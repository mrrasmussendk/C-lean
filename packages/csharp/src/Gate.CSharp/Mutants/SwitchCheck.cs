using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Mutants;

/// <summary>
/// Compiles a switched tree and names the mutants to back out: for each error or warning the
/// original tree did not have, the switch around it, else every switch in the same member,
/// else every switch in the file. `Everything` is returned when a problem has no place.
/// </summary>
internal static class SwitchCheck
{
    public const string Everything = "*";

    /// <summary>The errors and warnings of a tree, as rule, file and message (positions move when switches go
    /// in).</summary>
    public static IReadOnlySet<string> Problems(Solution solution) =>
        Diagnostics(solution).Select(Fingerprint).ToHashSet();

    public static IEnumerable<string> Offenders(Solution switched, IReadOnlySet<string> known) =>
        Diagnostics(switched).Where(problem => !known.Contains(Fingerprint(problem))).SelectMany(Blamed).Distinct();

    private static IEnumerable<Diagnostic> Diagnostics(Solution solution) =>
        solution.Files.Select(file => file.Model.Compilation).Distinct()
            .SelectMany(compilation => compilation.GetDiagnostics())
            .Where(problem => problem.Severity >= DiagnosticSeverity.Warning);

    private static string Fingerprint(Diagnostic problem) =>
        $"{problem.Id}|{problem.Location.SourceTree?.FilePath}|{problem.GetMessage()}";

    private static IEnumerable<string> Blamed(Diagnostic problem)
    {
        var root = problem.Location.SourceTree?.GetRoot();
        if (root is null)
        {
            return [Everything];
        }
        var around = root.FindNode(problem.Location.SourceSpan).AncestorsAndSelf().ToList();
        var member = around.OfType<MemberDeclarationSyntax>().FirstOrDefault();
        return new[] { Switches(around).Take(1), Switches(member?.DescendantNodes() ?? []),
            Switches(root.DescendantNodes()), [Everything] }
            .Select(ids => ids.ToList()).First(ids => ids.Count > 0);
    }

    private static IEnumerable<string> Switches(IEnumerable<SyntaxNode> nodes) =>
        nodes.OfType<ConditionalExpressionSyntax>().Select(Switcher.SwitchedKey).Where(key => key != "");
}
