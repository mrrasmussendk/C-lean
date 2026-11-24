// SZ05: Type SLOC must not exceed the limit
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class SZ05
{
    /// <summary>One declaration of a type; a partial type has several, and its size is their sum.</summary>
    private sealed record Part(SourceFile File, ISymbol Symbol, int Sloc);

    public static IEnumerable<Finding> Check(Solution solution, Settings settings)
    {
        var limit = settings.Limits["SZ05"];
        return solution.Files.SelectMany(Parts)
            .GroupBy(part => SymbolIds.Of(part.Symbol))
            .Select(parts => parts.First() with { Sloc = parts.Sum(part => part.Sloc) })
            .Where(type => type.Sloc > limit)
            .Select(
                type => Findings.OnSymbol("SZ05", type.File, type.Symbol, Message(type.Symbol.Name, type.Sloc, limit)));
    }

    private static IEnumerable<Part> Parts(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
            .Select(type => (Symbol: file.Model.GetDeclaredSymbol(type), Sloc: Sloc.Of(type)))
            .Where(type => type.Symbol is not null)
            .Select(type => new Part(file, type.Symbol!, type.Sloc));

    private static string Message(string name, int sloc, int limit) =>
        $"SZ05: type {name} has {sloc} SLOC, limit {limit}. Move a cohesive group of members into a type of its own.";
}
