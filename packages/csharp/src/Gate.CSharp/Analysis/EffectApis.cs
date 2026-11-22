using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

internal sealed record EffectUse(SyntaxNode Site, string Group, string Api);

/// <summary>
/// The one list of standard-library and runtime APIs that are effects. A key is a type
/// (every member is an effect) or a type and member. Mutable global state, the seventh
/// group, is found by StateWrites.
/// </summary>
internal static class EffectApis
{
    private const string Clock = "clock";
    private const string FileSystem = "file system";
    private const string Network = "network";
    private const string ConsoleGroup = "console";
    private const string Randomness = "randomness";
    private const string EnvironmentGroup = "environment";

    private static readonly IReadOnlyDictionary<string, string> Groups = new Dictionary<string, string>
    {
        ["System.DateTime.Now"] = Clock,
        ["System.DateTime.UtcNow"] = Clock,
        ["System.DateTime.Today"] = Clock,
        ["System.DateTimeOffset.Now"] = Clock,
        ["System.DateTimeOffset.UtcNow"] = Clock,
        ["System.TimeProvider"] = Clock,
        ["System.Diagnostics.Stopwatch"] = Clock,
        ["System.Environment.TickCount"] = Clock,
        ["System.Environment.TickCount64"] = Clock,
        ["System.IO.File"] = FileSystem,
        ["System.IO.Directory"] = FileSystem,
        ["System.IO.FileInfo"] = FileSystem,
        ["System.IO.DirectoryInfo"] = FileSystem,
        ["System.IO.FileStream"] = FileSystem,
        ["System.IO.StreamReader"] = FileSystem,
        ["System.IO.StreamWriter"] = FileSystem,
        ["System.IO.Path.GetTempPath"] = FileSystem,
        ["System.IO.Path.GetTempFileName"] = FileSystem,
        ["System.IO.Path.GetFullPath"] = FileSystem,
        ["System.Net.Http.HttpClient"] = Network,
        ["System.Net.WebClient"] = Network,
        ["System.Net.WebRequest"] = Network,
        ["System.Net.Dns"] = Network,
        ["System.Net.Sockets.Socket"] = Network,
        ["System.Net.Sockets.TcpClient"] = Network,
        ["System.Net.Sockets.TcpListener"] = Network,
        ["System.Net.Sockets.UdpClient"] = Network,
        ["System.Console"] = ConsoleGroup,
        ["System.Random"] = Randomness,
        ["System.Guid.NewGuid"] = Randomness,
        ["System.Security.Cryptography.RandomNumberGenerator"] = Randomness,
        ["System.Environment"] = EnvironmentGroup,
        ["System.AppContext"] = EnvironmentGroup,
        ["System.Diagnostics.Process"] = EnvironmentGroup,
    };

    public static IEnumerable<EffectUse> Uses(IEnumerable<SyntaxNode> nodes, SemanticModel model) =>
        nodes.Where(node => node is SimpleNameSyntax or BaseObjectCreationExpressionSyntax)
            .SelectMany(node => Use(node, model));

    private static IEnumerable<EffectUse> Use(SyntaxNode node, SemanticModel model)
    {
        var symbol = Symbols.Lookup(model, node);
        var owner = symbol is IMethodSymbol or IPropertySymbol or IFieldSymbol ? symbol.ContainingType : null;
        if (symbol is null || owner is null)
        {
            return [];
        }
        var type = owner.ToDisplayString();
        var api = Groups.ContainsKey($"{type}.{symbol.Name}") ? $"{type}.{symbol.Name}" : type;
        return Groups.TryGetValue(api, out var group) ? [new EffectUse(node, group, api)] : [];
    }
}
