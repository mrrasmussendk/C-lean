using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Parse;

namespace Gate.Cli;

/// <summary>`gate change new`, `gate change approve` and `gate conflict`.</summary>
internal static class ChangeCommands
{
    private const string ApprovalKey = "\napproval:";

    public static int New(IReadOnlyList<string> arguments, IReadOnlyList<string> words)
    {
        var path = Layout.In(Options.Root(arguments), Layout.Record(Id(words)));
        if (Files.Exists(path))
        {
            throw new GateFailure($"{path} exists");
        }

        Files.Write(path, Templates.ChangeRecord.Replace("{id}", Id(words)));
        Surroundings.Print($"wrote {path}");
        return C6Hardening.Passed;
    }

    public static int Approve(IReadOnlyList<string> arguments, IReadOnlyList<string> words)
    {
        var root = Options.Root(arguments);
        var approver = GitChange.UserName(root);
        if (InputReader.Config(root).Implementers.Contains(approver))
        {
            Surroundings.PrintError($"gate: approval refused: '{approver}' is an implementer identity");
            return C6Hardening.Blocked;
        }

        var path = Layout.In(root, Layout.Record(Id(words)));
        var body = Files.Read(path).Split(ApprovalKey)[0].TrimEnd();
        var digest = C1ChangeRecord.Digest(ChangeText.Record(path, body));
        Files.Write(path, $"{body}{ApprovalKey}\n  approver: {approver}\n  at: {Surroundings.Today()}\n  digest: {digest}\n");
        Surroundings.Print($"approved {path} as {approver}");
        return C6Hardening.Passed;
    }

    public static int Conflict(IReadOnlyList<string> arguments, IReadOnlyList<string> words)
    {
        var path = Layout.In(Options.Root(arguments), $"{C6Hardening.ConflictDirectory}{Surroundings.Stamp()}.yaml");
        var text = string.Join(' ', words).Replace("\"", "'");
        Files.Write(path, $"filed: {Surroundings.Today()}\nconflict: \"{text}\"\n");
        Surroundings.Print($"conflict filed in {path}; the change waits for a human");
        return C6Hardening.ConflictFiled;
    }

    private static string Id(IReadOnlyList<string> words) =>
        words.Count > 0 ? words[0] : throw new GateFailure("a change id is required");
}
