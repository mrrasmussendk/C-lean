using System.Collections.Generic;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Loading;

/// <summary>What the project file says. WarningsAsErrors is true when nullable warnings are errors and are not switched
/// off again; Symbols are the conditional-compilation symbols.</summary>
internal sealed record ProjectTraits(bool Nullable, bool WarningsAsErrors, bool ImplicitUsings, string FirstLine)
{
    public IReadOnlyList<string> Symbols { get; init; } = Preprocessor.Build;
}

/// <summary>One project to compile. Path is the csproj path, empty for sources that belong to no project
/// file.</summary>
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
