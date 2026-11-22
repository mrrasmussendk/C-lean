using System.Collections.Generic;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Loading;

internal sealed record ProjectTraits(bool Nullable, bool WarningsAsErrors, bool ImplicitUsings, string FirstLine);

/// <summary>One project to compile. Path is the csproj path, empty for sources that belong to no project file.</summary>
internal sealed record ProjectSpec(
    string Name,
    string Path,
    IReadOnlyList<TextFile> Sources,
    IReadOnlyList<string> References,
    IReadOnlyList<string> PackageAssemblies,
    ProjectTraits Traits);

internal sealed record ReferenceSet(
    IReadOnlyList<MetadataReference> Platform,
    IReadOnlyDictionary<string, MetadataReference> Packages);
