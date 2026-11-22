using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Model;

/// <summary>A text file as read. ByteOrderMark is the number of bytes before the text in the stored file.</summary>
internal sealed record TextFile(string Path, string Text, int ByteOrderMark = 0);

/// <summary>The command line. An argument the command does not take is the empty string.</summary>
internal sealed record Options(
    string Command,
    string Root,
    string Out,
    string Config,
    string Architecture,
    string Today,
    string Lines,
    string TraceFile);

internal sealed record Inputs(
    Options Options,
    string RootName,
    TextFile Config,
    TextFile Architecture,
    TextFile Lines,
    IReadOnlyList<TextFile> Tree);

internal sealed record Position(string File, int Line, int Column);

/// <summary>What a command produced: files to write under a directory, and failure lines for standard error.</summary>
internal sealed record Report(string Directory, IReadOnlyList<TextFile> Files, IReadOnlyList<string> Problems);

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
    bool Effect,
    bool Property);

internal sealed record Mutant(
    string Id,
    string File,
    int Line,
    int Column,
    string Mutator,
    int Start,
    int End,
    string Original,
    string Replacement,
    string Context);

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
