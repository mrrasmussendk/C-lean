using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Parse;

namespace Gate.Cli;

/// <summary>`c-lean change new`, `c-lean change approve` and `c-lean conflict`.</summary>
internal static class ChangeCommands
{
    private const string ApprovalKey = "\napproval:";

    public static int New(IReadOnlyList<string> arguments, IReadOnlyList<string> words)
    {
        var path = Path.Combine(Options.Value(arguments, Options.RootOption, Options.Here), Layout.Record(Id(words)));
        if (Files.Exists(path))
        {
            throw new GateFailure($"{path}: a change record with this id exists. Choose another id.");
        }

        Files.Write(path, Templates.ChangeRecord.Replace("{id}", Id(words)));
        Surroundings.Print($"wrote {path}");
        Surroundings.Print(Guide.AfterNewChange(Id(words)));
        return C6Hardening.Passed;
    }

    public static int Approve(IReadOnlyList<string> arguments, IReadOnlyList<string> words)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var approver = GitChange.UserName(root);
        if (InputReader.Config(root).Implementers.Contains(approver))
        {
            Surroundings.PrintError($"c-lean: approval refused: git user.name is '{approver}', which gate.yaml lists under `implementers`. A change record is approved by someone other than the implementer.");
            return C6Hardening.Blocked;
        }

        var path = Path.Combine(root, Layout.Record(Id(words)));
        if (!Files.Exists(path))
        {
            throw new GateFailure($"{path}: there is no such change record. `c-lean change new {Id(words)}` creates it.");
        }

        var body = Files.Read(path).Split(ApprovalKey)[0].TrimEnd();
        var digest = C1ChangeRecord.Digest(ChangeText.Record(path, body));
        Files.Write(path, $"{body}{ApprovalKey}\n  approver: {approver}\n  at: {Surroundings.Today()}\n  digest: {digest}\n");
        Surroundings.Print($"approved {path} as {approver}");
        return C6Hardening.Passed;
    }

    public static int Conflict(IReadOnlyList<string> arguments, IReadOnlyList<string> words)
    {
        var path = Path.Combine(Options.Value(arguments, Options.RootOption, Options.Here), $"{C6Hardening.ConflictDirectory}{Surroundings.Stamp()}.yaml");
        var text = string.Join(' ', words).Replace("\"", "'");
        Files.Write(path, $"filed: {Surroundings.Today()}\nconflict: \"{text}\"\n");
        Surroundings.Print($"conflict filed in {path}; the change waits for a human");
        return C6Hardening.ConflictFiled;
    }

    private static string Id(IReadOnlyList<string> words) =>
        words.Count > 0 ? words[0] : throw new GateFailure("the change id is missing. Usage: c-lean change new <id> | c-lean change approve <id>");
}
