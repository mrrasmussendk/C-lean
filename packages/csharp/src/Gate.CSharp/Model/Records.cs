using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Model;

internal sealed record TextFile(string Path, string Text);

internal sealed record Options(string Root, string Out, string Config, string Architecture, string Today);

internal sealed record Inputs(Options Options, string RootName, TextFile Config, TextFile Architecture, IReadOnlyList<TextFile> Tree);

internal sealed record Position(string File, int Line, int Column);

internal sealed record Report(string Sarif, string Graph, string Units, IReadOnlyList<string> Problems);

internal sealed record SourceFile(
    string Path,
    SyntaxTree Tree,
    SemanticModel Model,
    string Assembly,
    string Component,
    bool IsEffect,
    bool IsTest);

internal sealed record FunctionUnit(
    SourceFile File,
    SyntaxNode Node,
    SyntaxNode Body,
    IMethodSymbol Symbol,
    string Id,
    string Name,
    int Line);

internal sealed record ProjectConfig(string Path, string FirstLine, bool IsStrict, bool IsTest);

internal sealed record Solution(
    IReadOnlyList<SourceFile> Files,
    IReadOnlyList<FunctionUnit> Units,
    IReadOnlyList<ProjectConfig> Projects,
    IReadOnlyList<string> Problems);

internal sealed record Finding(string Rule, string File, int Line, string Message, string Key, bool Suppressed);

internal sealed record GlossaryTerm(string Term, IReadOnlyList<string> Banned);

internal sealed record Settings(
    IReadOnlyDictionary<string, int> Limits,
    IReadOnlyDictionary<string, string> Severities,
    string Today,
    IReadOnlyList<GlossaryTerm> Glossary);

internal sealed record GraphNode(
    string Id,
    string Kind,
    string Component,
    string File,
    int Line,
    bool Test,
    bool Public,
    bool Effect);

internal sealed record GraphEdge(string From, string To, string Kind);

internal sealed record CodeGraph(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges);

internal sealed record SequenceRow(string Hash, int StartLine, int EndLine);

internal sealed record UnitRow(
    string Id,
    string File,
    int StartLine,
    int EndLine,
    int Sloc,
    string Hash,
    bool Test,
    int Callers,
    IReadOnlyList<SequenceRow> Sequences);
