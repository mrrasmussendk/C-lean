// NM01: Glossary: an identifier must not use a word the glossary lists as a banned synonym of a domain term
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class NM01
{
    private static readonly System.Text.RegularExpressions.Regex Word = new("[A-Z]+(?![a-z])|[A-Z]?[a-z]+");
    private static readonly string[] PluralEndings = ["", "s", "es"];

    private sealed record Use(ISymbol Symbol, string Word, string Term);

    public static IEnumerable<Finding> Check(SourceFile file, Settings settings) =>
        file.Tree.GetRoot().DescendantTokens()
            .Where(token => token.IsKind(SyntaxKind.IdentifierToken) && token.Parent is not null)
            .Select(token => (Token: token, Symbol: file.Model.GetDeclaredSymbol(token.Parent!)))
            .Where(declared => declared.Symbol?.Name == declared.Token.ValueText)
            .SelectMany(declared => BannedUses(declared.Symbol!, settings.Glossary))
            .Select(use => Findings.OnSymbol("NM01", file, use.Symbol, Message(use)));

    private static string Message(Use use) =>
        $"NM01: identifier {use.Symbol.Name} uses '{use.Word}', a banned synonym of the domain term '{use.Term}'. "
        + $"Rename it using '{use.Term}'.";

    private static IEnumerable<Use> BannedUses(ISymbol symbol, IReadOnlyList<GlossaryTerm> glossary)
    {
        var words = Word.Matches(symbol.Name).Select(match => match.Value.ToLowerInvariant()).ToList();
        return glossary.SelectMany(term => term.Banned.Where(banned => words.Any(word => IsFormOf(word, banned)))
            .Select(banned => new Use(symbol, banned, term.Term)));
    }

    private static bool IsFormOf(string word, string banned) => PluralEndings.Any(ending => word == banned + ending);
}
