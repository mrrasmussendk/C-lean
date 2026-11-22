using System;
using System.Collections.Generic;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;

namespace Gate.CSharp.Pipeline;

/// <summary>The explicit list of commands. Each is a function of what was read and returns what to write.</summary>
internal static class Commands
{
    public static readonly IReadOnlyDictionary<string, Func<Inputs, ReferenceSet, Report>> All =
        new Dictionary<string, Func<Inputs, ReferenceSet, Report>>
        {
            ["analyze"] = Analyzer.Analyze,
            ["mutants"] = MutantLister.List,
            ["instrument"] = TraceInstrumenter.Instrument,
        };
}
