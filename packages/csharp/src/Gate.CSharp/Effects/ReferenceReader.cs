using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Effects;

/// <summary>Loads metadata references: the assemblies of the running runtime and the restored packages of the
/// projects.</summary>
internal static class ReferenceReader
{
    private const string CoreLibrary = "System.Private.CoreLib.dll";

    public static ReferenceSet Load(Inputs inputs)
    {
        var trusted = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "").Split(Path.PathSeparator);
        var runtime = trusted.Where(path => Path.GetFileName(path) == CoreLibrary)
            .Select(Path.GetDirectoryName)
            .FirstOrDefault();
        var platform = trusted.Where(path => Path.GetDirectoryName(path) == runtime)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList();
        var packages = ProjectLayout.Of(inputs.Tree, inputs.RootName).SelectMany(project => project.PackageAssemblies)
            .Distinct().Where(File.Exists)
            .ToDictionary(path => path, path => (MetadataReference)MetadataReference.CreateFromFile(path));
        return new ReferenceSet(platform, packages);
    }
}
