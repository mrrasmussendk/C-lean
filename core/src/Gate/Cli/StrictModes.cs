using System.Text.RegularExpressions;

namespace Gate.Cli;

/// <summary>
/// Turns on the strict modes the rules depend on, as edits of project files' text.
/// Each edit only adds or raises a setting; a file that is already strict comes back unchanged.
/// </summary>
internal static class StrictModes
{
    private const string PropertyGroup = "<PropertyGroup>";
    private const string CompilerOptions = @"""compilerOptions""\s*:\s*\{";
    private const string Pyright = "\n[tool.pyright]\ntypeCheckingMode = \"strict\"\n";

    /// <summary>The strict text of a project file, by the kind its name shows; other files come back unchanged.</summary>
    public static string Apply(string path, string text) =>
        path.EndsWith(".csproj") ? CSharp(text)
        : Path.GetFileName(path) == "tsconfig.json" ? TypeScript(text)
        : Path.GetFileName(path) == "pyproject.toml" ? Python(text)
        : text;

    /// <summary>Nullable reference types on, with warnings as errors.</summary>
    private static string CSharp(string project)
    {
        var enabled = Regex.Replace(project, @"<Nullable>\s*(disable|annotations|warnings)\s*</Nullable>", "<Nullable>enable</Nullable>");
        var raised = Regex.Replace(enabled, @"<TreatWarningsAsErrors>\s*false\s*</TreatWarningsAsErrors>", "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
        var missing = new[] { "<Nullable>enable</Nullable>", "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>" }
            .Where(setting => !raised.Contains(setting.Split('>')[0]));
        var added = string.Concat(missing.Select(setting => $"\n    {setting}"));
        var at = raised.IndexOf(PropertyGroup, StringComparison.Ordinal);
        return at < 0 || added.Length == 0 ? raised : raised.Insert(at + PropertyGroup.Length, added);
    }

    /// <summary>`strict` on in compilerOptions.</summary>
    private static string TypeScript(string config)
    {
        var raised = Regex.Replace(config, @"""strict""\s*:\s*false", "\"strict\": true");
        return raised.Contains("\"strict\"")
            ? raised
            : new Regex(CompilerOptions).Replace(raised, match => $"{match.Value}\n    \"strict\": true,", 1);
    }

    /// <summary>A type checker in strict mode: pyright, unless the project already configures pyright or mypy.</summary>
    private static string Python(string project) =>
        project.Contains("[tool.pyright]") || project.Contains("[tool.mypy]") ? project : project.TrimEnd() + "\n" + Pyright;
}
