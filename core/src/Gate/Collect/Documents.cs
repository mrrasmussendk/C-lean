using Gate.Effects;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Reads and parses one file. A file that is wrong fails the gate with its path, position and reason.</summary>
internal static class Documents
{
    public static T Read<T>(string path, Func<string, T> parse)
    {
        try
        {
            return parse(Files.Read(path));
        }
        catch (Exception problem) when (Malformed.IsSyntax(problem))
        {
            throw new GateFailure($"{path}:{Malformed.Syntax(problem)}. Fix the file and run the gate again.");
        }
    }

    /// <summary>Fails with every problem a validator found, one per line, each prefixed with the file.</summary>
    public static void Require(string path, Func<string, IReadOnlyList<string>> problems)
    {
        var found = Read(path, problems);
        if (found.Count > 0)
        {
            throw new GateFailure(string.Join("\n      ", found.Select(problem => $"{path}:{problem}")));
        }
    }
}
