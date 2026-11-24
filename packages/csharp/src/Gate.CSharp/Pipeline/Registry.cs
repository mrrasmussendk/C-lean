using System;
using System.Collections.Generic;
using Gate.CSharp.Model;
using Gate.CSharp.Rules;

namespace Gate.CSharp.Pipeline;

internal sealed record FunctionRule(string Id, string Scope, Func<FunctionUnit, Settings, IEnumerable<Finding>> Check);

internal sealed record FileRule(string Id, string Scope, Func<SourceFile, Settings, IEnumerable<Finding>> Check);

internal sealed record SolutionRule(string Id, string Scope, Func<Solution, Settings, IEnumerable<Finding>> Check);

/// <summary>
/// The explicit list of rules. Scope says which code a rule sees: production code, test code or both.
/// HD01 is not listed: it runs last, on the findings of the rules listed here (see RuleRunner).
/// </summary>
internal static class Registry
{
    public const string Production = "production";
    public const string Tests = "tests";
    public const string All = "all";

    public static readonly IReadOnlyList<FunctionRule> FunctionRules =
    [
        new("SZ01", Production, SZ01.Check),
        new("SZ02", Production, SZ02.Check),
        new("SZ03", Production, SZ03.Check),
        new("AR01", Production, AR01.Check),
        new("AR02", Production, AR02.Check),
        new("FX02", Production, FX02.Check),
        new("ER03", Production, ER03.Check),
        new("FR02", Production, FR02.Check),
        new("TS01", Tests, TS01.Check),
        new("TS02", Tests, TS02.Check),
    ];

    public static readonly IReadOnlyList<FileRule> FileRules =
    [
        new("SZ06", Production, SZ06.Check),
        new("FX01", Production, FX01.Check),
        new("FX03", Production, FX03.Check),
        new("ER01", Production, ER01.Check),
        new("CP01", Production, CP01.Check),
        new("DC02", All, DC02.Check),
        new("DC03", Production, DC03.Check),
        new("NM01", Production, NM01.Check),
    ];

    public static readonly IReadOnlyList<SolutionRule> SolutionRules =
    [
        new("SZ04", Production, SZ04.Check),
        new("SZ05", Production, SZ05.Check),
        new("ER02", Production, ER02.Check),
        new("DC01", Production, DC01.Check),
    ];
}
