namespace Gate.Model;

/// <summary>Resolves a rule's limit and severity: gate.yaml first, then the rule table.</summary>
internal static class Settings
{
    public static int Limit(Config config, string rule) =>
        config.Rules.TryGetValue(rule, out var setting) && setting.Limit > 0
            ? setting.Limit
            : DefaultLimit(rule);

    public static Severity Severity(Config config, string rule) =>
        config.Rules.TryGetValue(rule, out var setting) && setting.Severity.Length > 0
            ? Severities.Named(setting.Severity)
            : Entry(rule).Severity;

    private static int DefaultLimit(string rule) =>
        int.Parse(new string(Entry(rule).Default.TakeWhile(char.IsDigit).ToArray()));

    private static RuleEntry Entry(string rule) =>
        RuleTable.Entries.First(entry => entry.Id == rule);
}
