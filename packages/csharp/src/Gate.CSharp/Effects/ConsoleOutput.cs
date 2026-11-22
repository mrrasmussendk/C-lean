using System;
using System.Collections.Generic;
using Gate.CSharp.Output;

namespace Gate.CSharp.Effects;

/// <summary>The only code that writes to the console.</summary>
internal static class ConsoleOutput
{
    public static void Version() => Console.Out.WriteLine($"{Sarif.PackageVersion} spec {Sarif.SpecVersion}");

    public static void Problems(IReadOnlyList<string> lines) => Console.Error.WriteLine(string.Join("\n", lines));
}
