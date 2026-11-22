using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Config;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gate.CSharp.Loading;

/// <summary>Builds one compilation per project, referenced projects first, and the analyzable files of each.</summary>
internal static class Compiler
{
    private const string ImplicitUsings =
        "global using System; global using System.Collections.Generic; global using System.IO; global using "
            + "System.Linq; "
        + "global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;";

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    public static Solution Build(
        IReadOnlyList<ProjectSpec> projects,
        ReferenceSet references,
        Classification classification)
    {
        var ordered = projects.OrderBy(project => Depth(project, projects)).ToList();
        var compilations = ordered.Aggregate(
            ImmutableDictionary<string, CSharpCompilation>.Empty,
            (built, project) => built.SetItem(project.Path, Compile(project, references, built)));
        var trees = compilations.Values.SelectMany(
            compilation => compilation.SyntaxTrees.Select(tree => (Compilation: compilation, Tree: tree)))
            .Where(source => source.Tree.FilePath != "")
            .OrderBy(source => source.Tree.FilePath, System.StringComparer.Ordinal).ToList();
        var files = trees.Where(source => !SyntaxErrors(source.Tree).Any() && !IsGenerated(source.Tree, classification))
            .Select(source => File(source.Compilation, source.Tree, classification)).ToList();
        var configs = ordered.Where(project => project.Path != "")
            .Select(project => Config(project, classification))
            .ToList();
        var problems = trees.SelectMany(source => SyntaxErrors(source.Tree)).Select(Problem).ToList();
        return new Solution(files, files.SelectMany(Functions.Of).ToList(), configs, problems);
    }

    private static int Depth(ProjectSpec project, IReadOnlyList<ProjectSpec> all) =>
        1 + all.Where(other => project.References.Contains(other.Path) && other.Path != project.Path)
            .Select(other => Depth(other, all)).DefaultIfEmpty(0).Max();

    private static CSharpCompilation Compile(
        ProjectSpec project,
        ReferenceSet references,
        ImmutableDictionary<string, CSharpCompilation> built)
    {
        var implicitUsings = new[] { ImplicitUsings }.Where(_ => project.Traits.ImplicitUsings)
            .Select(text => CSharpSyntaxTree.ParseText(text, ParseOptions));
        var trees = project.Sources.Select(source => CSharpSyntaxTree.ParseText(source.Text, ParseOptions, source.Path))
            .Concat(implicitUsings);
        var packages = project.PackageAssemblies.Where(references.Packages.ContainsKey)
            .Select(path => references.Packages[path]);
        var projects = project.References.Where(built.ContainsKey)
            .Select(path => (MetadataReference)built[path].ToMetadataReference());
        var nullable = project.Traits.Nullable ? NullableContextOptions.Enable : NullableContextOptions.Disable;
        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: nullable);
        return CSharpCompilation.Create(
            project.Name,
            trees,
            references.Platform.Concat(packages).Concat(projects),
            options);
    }

    private static IEnumerable<Diagnostic> SyntaxErrors(SyntaxTree tree) =>
        tree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    private static string Problem(Diagnostic error)
    {
        var start = error.Location.GetLineSpan().StartLinePosition;
        var position = new Position(error.Location.SourceTree?.FilePath ?? "", start.Line + 1, start.Character + 1);
        return Failures.Line(
            position,
            $"source file does not parse: {error.GetMessage().TrimEnd('.')}",
            "Fix the syntax; the file was left out of this analysis");
    }

    private static bool IsGenerated(SyntaxTree tree, Classification classification) =>
        Classify.IsGenerated(tree.FilePath, tree.GetText().Lines.Select(line => line.ToString()), classification);

    private static SourceFile File(CSharpCompilation compilation, SyntaxTree tree, Classification classification)
    {
        var component = Classify.ComponentOf(tree.FilePath, classification);
        return new SourceFile(
            tree.FilePath,
            tree,
            compilation.GetSemanticModel(tree),
            compilation.AssemblyName ?? "",
            component.Name,
            component.Effect,
            Classify.IsTest(tree.FilePath, classification));
    }

    private static ProjectConfig Config(ProjectSpec project, Classification classification) => new(
        project.Path,
        project.Traits.FirstLine,
        project.Traits.Nullable && project.Traits.WarningsAsErrors,
        Classify.IsTest(project.Path, classification));
}
