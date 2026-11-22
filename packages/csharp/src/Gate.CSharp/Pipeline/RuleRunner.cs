using System;
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Gate.CSharp.Rules;

namespace Gate.CSharp.Pipeline;

/// <summary>Runs every registered rule on the code in its scope, then HD01, and marks suppressed findings.</summary>
internal static class RuleRunner
{
    public static IReadOnlyList<Finding> Run(Solution solution, Settings settings)
    {
        var onFunctions = solution.Units.SelectMany(unit =>
            Registry.FunctionRules.Where(rule => InScope(rule.Scope, unit.File)).SelectMany(rule => rule.Check(unit, settings)));
        var onFiles = solution.Files.SelectMany(file =>
            Registry.FileRules.Where(rule => InScope(rule.Scope, file)).SelectMany(rule => rule.Check(file, settings)));
        var onSolution = Registry.SolutionRules.SelectMany(rule => rule.Check(Scoped(solution, rule.Scope), settings));
        var found = onFunctions.Concat(onFiles).Concat(onSolution).Distinct().ToList();
        var valid = solution.Files.SelectMany(Suppressions.In).Where(suppression => Suppressions.IsValid(suppression, settings.Today)).ToList();
        return Suppressions.Mark(found, valid).Concat(HD01.Check(solution, found, settings))
            .OrderBy(finding => finding.File, StringComparer.Ordinal).ThenBy(finding => finding.Line)
            .ThenBy(finding => finding.Rule, StringComparer.Ordinal).ToList();
    }

    private static bool InScope(string scope, SourceFile file) =>
        scope == Registry.All || (scope == Registry.Tests) == file.IsTest;

    private static Solution Scoped(Solution solution, string scope) => solution with
    {
        Files = solution.Files.Where(file => InScope(scope, file)).ToList(),
        Units = solution.Units.Where(unit => InScope(scope, unit.File)).ToList(),
    };
}
