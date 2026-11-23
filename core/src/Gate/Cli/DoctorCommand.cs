using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Model;

namespace Gate.Cli;

/// <summary>`c-lean doctor`: version skew between the core, the native packages and the configured spec.</summary>
internal static class DoctorCommand
{
    public const string CoreVersion = "0.1.0";
    public const string SpecVersion = "1.0";

    public static int Run(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var config = InputReader.Config(root);
        Surroundings.Print($"core {CoreVersion} spec {SpecVersion}");
        Surroundings.Print($"{Layout.Config} spec {config.Spec}");
        var specs = config.Languages.Select(language => PackageSpec(root, config, language)).Append(config.Spec).ToList();
        var skewed = specs.Any(spec => Major(spec) != Major(SpecVersion));
        Surroundings.Print(specs.Contains("") ? "not ready: a package this repository needs is not installed"
            : skewed ? "skew: spec major versions differ; packages on different major versions do not run together"
            : "no skew");
        return skewed ? C6Hardening.GateFailed : C6Hardening.Passed;
    }

    /// <summary>A package answers `--version` with `&lt;package version&gt; spec &lt;spec version&gt;`. Empty when it is not installed.</summary>
    private static string PackageSpec(string root, Config config, string language)
    {
        var result = Processes.Run(new ProcessCall(NativeRunner.Command(root, config, language), ["--version"], root));
        var answer = result.ExitCode == 0 ? result.Output.Trim() : "not installed";
        Surroundings.Print($"gate-{language} {answer}");
        if (result.ExitCode != 0)
        {
            Surroundings.Print(Guide.Missing(language));
        }

        return result.ExitCode == 0 ? answer.Split(' ').Last() : "";
    }

    private static string Major(string version) => version.Split('.')[0];
}
