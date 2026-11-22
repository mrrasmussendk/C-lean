using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Model;

namespace Gate.CSharp.Config;

internal sealed record RuleInfo(string Id, string Severity, int Limit);

/// <summary>Every rule ID of spec 1.0 with its default severity and limit (0 when the rule has none).</summary>
internal static class RuleTable
{
    private const string Block = "block";
    private const string Warn = "warn";
    private const string Report = "report";
    private const int FunctionSloc = 30;
    private const int Complexity = 15;
    private const int Nesting = 3;
    private const int Parameters = 4;
    private const int TypeSloc = 200;
    private const int LineLength = 120;
    private const int DesignSpread = 2;
    private const int CloneStatements = 6;
    private const int ForwardedPercent = 80;
    private const int ExtractBalance = 5;
    private const int IndirectionDepth = 6;
    private const int TestCloneStatements = 4;

    public static readonly IReadOnlyList<RuleInfo> All =
    [
        new("SZ01", Block, FunctionSloc), new("SZ02", Block, Complexity), new("SZ03", Block, Nesting),
        new("SZ04", Block, Parameters), new("SZ05", Warn, TypeSloc), new("SZ06", Report, LineLength),
        new("AR01", Block, 0), new("AR02", Block, 0),
        new("FX01", Block, 0), new("FX02", Warn, 0), new("FX03", Warn, 0),
        new("DU01", Block, CloneStatements),
        new("ER01", Block, 0), new("ER02", Block, 0), new("ER03", Report, 0),
        new("CP01", Warn, 1),
        new("DC01", Block, 0), new("DC02", Block, 0), new("DC03", Warn, 0),
        new("FR01", Warn, 0), new("FR02", Warn, ForwardedPercent), new("FR03", Warn, ExtractBalance),
        new("FR04", Report, IndirectionDepth),
        new("NM01", Warn, 0),
        new("TS01", Block, 0), new("TS02", Block, 0), new("TS03", Block, TestCloneStatements),
        new("HD01", Block, 0), new("HD02", Block, 0), new("HD03", Block, 0),
        new("GR01", Block, 0),
        new("DS01", Report, DesignSpread), new("DS02", Report, DesignSpread),
    ];

    public static bool IsKnown(string id) => All.Any(rule => rule.Id == id);

    public static Settings Settings(GateConfig config, string today, IReadOnlyList<GlossaryTerm> glossary) =>
        new(
            All.ToDictionary(rule => rule.Id, rule => config.Limits.GetValueOrDefault(rule.Id, rule.Limit)),
            All.ToDictionary(rule => rule.Id, rule => config.Severities.GetValueOrDefault(rule.Id, rule.Severity)),
            today,
            glossary);
}
