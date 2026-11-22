using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;

namespace Gate.CSharp.Loading;

/// <summary>
/// Finds the projects under the root without MSBuild: one per csproj, holding the sources in its
/// directory that no deeper csproj owns. Sources owned by no csproj form one project named after the root.
/// </summary>
internal static class ProjectLayout
{
    public static IReadOnlyList<ProjectSpec> Of(IReadOnlyList<TextFile> tree, string rootName)
    {
        var projects = tree.Where(file => file.Path.EndsWith(".csproj")).ToList();
        var sources = tree.Where(file => file.Path.EndsWith(".cs")).ToLookup(file => OwnerOf(file.Path, projects));
        var loose = new ProjectSpec(
            rootName,
            "",
            sources[""].ToList(),
            [],
            [],
            new ProjectTraits(true, true, false, ""));
        return projects.Select(project => Spec(project, sources[project.Path].ToList(), tree))
            .Concat(new[] { loose }.Where(project => project.Sources.Count > 0))
            .ToList();
    }

    private static string OwnerOf(string source, IReadOnlyList<TextFile> projects) =>
        projects.Select(project => project.Path)
            .Where(project => source.StartsWith(Paths.Directory(project)))
            .OrderByDescending(project => Paths.Directory(project).Length)
            .FirstOrDefault("");

    private static ProjectSpec Spec(TextFile project, IReadOnlyList<TextFile> sources, IReadOnlyList<TextFile> tree)
    {
        var directory = Paths.Directory(project.Path);
        var own = Document(project);
        var settings = new[] { own }.Concat(Inherited(directory, tree).Select(Document)).ToList();
        var assets = tree.Where(file => file.Path == $"{directory}obj/project.assets.json")
            .SelectMany(file => Assets.CompileAssemblies(file.Text));
        var references = own.Descendants().Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => Paths.Resolve(directory, element.Attribute("Include")?.Value ?? ""));
        var name = own.Descendants()
            .Where(element => element.Name.LocalName == "AssemblyName")
            .Select(element => element.Value.Trim())
            .FirstOrDefault("");
        return new ProjectSpec(
            name != "" ? name : System.IO.Path.GetFileNameWithoutExtension(project.Path),
            project.Path,
            sources,
            references.ToList(),
            assets.ToList(),
            Traits(settings, project.Text));
    }

    private static XDocument Document(TextFile file)
    {
        try
        {
            return XDocument.Parse(file.Text);
        }
        catch (XmlException error)
        {
            var position = new Position(file.Path, error.LineNumber, error.LinePosition);
            throw Failures.At(
                position,
                $"malformed project XML: {error.Message.TrimEnd('.')}",
                "Fix the XML at this position");
        }
    }

    private static IEnumerable<TextFile> Inherited(string directory, IReadOnlyList<TextFile> tree) =>
        tree.Where(
            file => file.Path.EndsWith("Directory.Build.props") && directory.StartsWith(Paths.Directory(file.Path)))
            .OrderByDescending(file => file.Path.Length);

    private static ProjectTraits Traits(IReadOnlyList<XDocument> settings, string text) => new(
        Property(settings, "Nullable") == "enable",
        Property(settings, "TreatWarningsAsErrors") == "true"
            || Property(settings, "WarningsAsErrors").Contains("nullable"),
        Property(settings, "ImplicitUsings") is "enable" or "true",
        text.Split('\n').First().Trim());

    /// <summary>The lower-case value of an MSBuild property from the first document that sets it; empty when none
    /// does.</summary>
    private static string Property(IEnumerable<XDocument> settings, string name) =>
        settings.SelectMany(document => document.Descendants())
            .Where(element => element.Name.LocalName == name)
            .Select(element => element.Value.Trim().ToLowerInvariant())
            .FirstOrDefault("");
}
