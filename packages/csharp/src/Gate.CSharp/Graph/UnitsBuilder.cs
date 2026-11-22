using System;
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Graph;

/// <summary>One unit per function with a body: size, normalized hash, callers and statement-sequence hashes.</summary>
internal static class UnitsBuilder
{
    public static IReadOnlyList<UnitRow> Build(Solution solution, CodeGraph graph, Settings settings)
    {
        var production = graph.Nodes.Where(node => !node.Test).Select(node => node.Id).ToHashSet();
        var callers = graph.Edges.Where(edge => edge.Kind == "call" && production.Contains(edge.From)).ToLookup(edge => edge.To, edge => edge.From);
        return solution.Units
            .Select(unit => Row(unit, callers[unit.Id].Distinct().Count(), settings.Limits[unit.File.IsTest ? "TS03" : "DU01"]))
            .OrderBy(row => row.Id, StringComparer.Ordinal).ThenBy(row => row.StartLine).ToList();
    }

    private static UnitRow Row(FunctionUnit unit, int callers, int window)
    {
        var sequences = Functions.OwnNodes(unit.Body).Select(Statements)
            .Where(statements => statements.Count >= window)
            .SelectMany(statements => Enumerable.Range(0, statements.Count - window + 1).Select(start => statements.Skip(start).Take(window).ToList()))
            .Select(run => new SequenceRow(NormalizedHash.Of(run), Lines.Of(run.First()), Lines.EndOf(run.Last().GetLocation())));
        return new UnitRow(
            unit.Id,
            unit.File.Path,
            Lines.Of(unit.Node),
            Lines.EndOf(unit.Node.GetLocation()),
            Sloc.Of(unit.Body),
            NormalizedHash.Of([unit.Body]),
            unit.File.IsTest,
            callers,
            sequences.ToList());
    }

    private static IReadOnlyList<StatementSyntax> Statements(SyntaxNode node) => node switch
    {
        BlockSyntax block => block.Statements,
        SwitchSectionSyntax section => section.Statements,
        _ => [],
    };
}
