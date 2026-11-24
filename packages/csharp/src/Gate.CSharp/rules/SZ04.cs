// SZ04: A function must not declare more parameters than the limit
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class SZ04
{
    /// <summary>A parameter list that belongs to no function with a body: a primary constructor or an
    /// indexer.</summary>
    private sealed record Signature(SourceFile File, ISymbol Symbol, int Count);

    public static IEnumerable<Finding> Check(Solution solution, Settings settings)
    {
        var limit = settings.Limits["SZ04"];
        var functions = solution.Units
            .Select(unit => (Unit: unit, Count: unit.Symbol.AssociatedSymbol is null ? Count(unit.Symbol) : 0))
            .Where(function => function.Count > limit)
            .Select(
                function =>
                    Findings.OnFunction("SZ04", function.Unit, Message(function.Unit.Name, function.Count, limit)));
        var signatures = solution.Files.SelectMany(Signatures)
            .Where(signature => signature.Count > limit)
            .Select(
                signature => Findings.OnSymbol(
                    "SZ04",
                    signature.File,
                    signature.Symbol,
                    Message(signature.Symbol.Name, signature.Count, limit)));
        return functions.Concat(signatures);
    }

    private static string Message(string name, int count, int limit) =>
        $"SZ04: function {name} declares {count} parameters, limit {limit}. "
        + "Group the parameters that travel together into one type, or split the function.";

    private static int Count(IMethodSymbol function) =>
        function.Parameters.Count(parameter => !Symbols.IsReceiver(parameter));

    /// <summary>The primary constructors of classes and structs, and the indexers. A positional record is exempt: its
    /// parameter list is the declaration of its data.</summary>
    private static IEnumerable<Signature> Signatures(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes().SelectMany(node => node switch
        {
            RecordDeclarationSyntax => [],
            TypeDeclarationSyntax { ParameterList: not null } type => Primary(file, type),
            IndexerDeclarationSyntax indexer => Indexer(file, indexer),
            _ => Enumerable.Empty<Signature>(),
        });

    private static IEnumerable<Signature> Primary(SourceFile file, TypeDeclarationSyntax type) =>
        file.Model.GetDeclaredSymbol(type) is { } declared && Symbols.PrimaryConstructor(declared) is { } constructor
            ? [new Signature(file, constructor, constructor.Parameters.Length)]
            : [];

    private static IEnumerable<Signature> Indexer(SourceFile file, IndexerDeclarationSyntax indexer) =>
        file.Model.GetDeclaredSymbol(indexer) is { } declared
            ? [new Signature(file, declared, declared.Parameters.Length)]
            : [];
}
