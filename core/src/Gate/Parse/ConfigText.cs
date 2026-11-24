using Gate.Model;
using YamlDotNet.RepresentationModel;

namespace Gate.Parse;

/// <summary>gate.yaml and architecture.yaml.</summary>
internal static class ConfigText
{
    private const double DefaultBudgetSeconds = 5;
    private const int DefaultVotes = 5;

    public static Config Config(string text)
    {
        var root = Yaml.Root(text);
        return new Config(Yaml.Text(root, "spec"), Yaml.Texts(root, "languages"), Yaml.Texts(root, "implementers"), Rules(root), Yaml.Texts(root, "protected").Append(IgnoreFile).Distinct().ToList())
        {
            Tests = Tests(Yaml.Map(root, "tests")),
            Judge = Judge(Yaml.Map(root, "judge")),
            Commands = Commands(Yaml.Map(root, "packages")),
            Excluded = Yaml.Texts(Yaml.Map(root, "paths"), "excluded"),
        };
    }

    public static Architecture Architecture(string text)
    {
        var root = Yaml.Root(text);
        var components = Yaml.Maps(root, "components").Select(Component).ToList();
        return new Architecture(components, Edges(root, "edges"));
    }

    public static IReadOnlyList<ComponentEdge> Edges(YamlMappingNode map, string key) =>
        Yaml.Maps(map, key).Select(edge => new ComponentEdge(Yaml.Text(edge, "from"), Yaml.Text(edge, "to"))).ToList();

    private static Component Component(YamlMappingNode map) =>
        new(Yaml.Text(map, "name"), Yaml.Texts(map, "paths"), Yaml.Flag(map, "effect"), Yaml.Flag(map, "boundary"))
        {
            CoreLogic = Yaml.Flag(map, "core_logic"),
        };

    private static IReadOnlyDictionary<string, RuleSetting> Rules(YamlMappingNode root)
    {
        var rules = Yaml.Map(root, "rules");
        return Yaml.Keys(rules).ToDictionary(id => id, id => Rule(Yaml.Map(rules, id)));
    }

    private static RuleSetting Rule(YamlMappingNode map) =>
        new(Yaml.Number(map, "limit"), Yaml.Text(map, "severity"));

    private static TestSettings Tests(YamlMappingNode map)
    {
        var budget = double.TryParse(Yaml.Text(map, "runtime_budget_seconds"), out var seconds) ? seconds : DefaultBudgetSeconds;
        return new TestSettings(budget, Yaml.Text(map, "missing_report"))
        {
            Commands = Table(Yaml.Map(map, "commands")),
            Setup = Table(Yaml.Map(map, "setup")),
        };
    }

    private static IReadOnlyDictionary<string, string> Table(YamlMappingNode map) =>
        Yaml.Keys(map).ToDictionary(key => key, key => Yaml.Text(map, key));

    private static JudgeSettings Judge(YamlMappingNode map)
    {
        var votes = Yaml.Number(map, "votes") > 0 ? Yaml.Number(map, "votes") : DefaultVotes;
        return new JudgeSettings(votes, Yaml.Text(map, "model"), Yaml.Texts(map, "command"), Yaml.Text(map, "fail_history"));
    }

    private static IReadOnlyDictionary<string, string> Commands(YamlMappingNode packages) =>
        Yaml.Keys(packages).ToDictionary(language => language, language => Yaml.Text(Yaml.Map(packages, language), "command"));

    /// <summary>The ignore file decides what the gate does not judge, so it is protected whether gate.yaml lists it or not.</summary>
    private const string IgnoreFile = ".c-leanignore";
}
