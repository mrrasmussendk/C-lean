namespace Gate.Cli;

/// <summary>How text is coloured: with terminal escape codes for a person watching, or not at all for a log or an agent.</summary>
internal sealed record Pen(string Format)
{
    public static readonly Pen Colours = new("\u001b[{colour}m{text}\u001b[0m");
    public static readonly Pen Plain = new("{text}");

    public string Paint(string text, string colour) => Format.Replace("{colour}", colour).Replace("{text}", text);
}

/// <summary>The pictures of the console. Pure text: the caller shows them only when a person is watching.</summary>
internal static class Art
{
    public const string Green = "32";
    public const string Yellow = "33";
    public const string Red = "31";
    public const string Cyan = "36";
    public const string Bold = "1";

    private const string Name = """
           ____           _
          / ___|         | |   ___    __ _   _ __
         | |      _____  | |  / _ \  / _` | | '_ \
         | |___  |_____| | | |  __/ | (_| | | | | |
          \____|         |_|  \___|  \__,_| |_| |_|
        """;

    private const string Open = """
          ||=             =||
          ||  \         /  ||   {word}
          ||   \       /   ||   {meaning}
          ||=             =||
        """;

    private const string Closed = """
          ||======||======||
          ||  ||  ||  ||  ||   {word}
          ||  ||  ||  ||  ||   {meaning}
          ||======||======||
        """;

    /// <summary>The name of the project, C-lean, in large letters, with a line under it.</summary>
    public static IReadOnlyList<string> Banner(Pen pen, string caption) =>
        ["", .. Rows(Name).Select(row => pen.Paint(row, Cyan)), $"  {caption}"];

    /// <summary>A gate standing open, with the verdict beside it.</summary>
    public static IReadOnlyList<string> Passed(Pen pen, string meaning) =>
        [.. Rows(Open.Replace("{word}", "PASSED").Replace("{meaning}", meaning)).Select(row => pen.Paint(row, Green)), ""];

    /// <summary>A gate standing shut, with the verdict beside it.</summary>
    public static IReadOnlyList<string> Stopped(Pen pen, string meaning) =>
        [.. Rows(Closed.Replace("{word}", "STOPPED").Replace("{meaning}", meaning)).Select(row => pen.Paint(row, Red)), ""];

    /// <summary>A ticked line of a checklist.</summary>
    public static string Ticked(Pen pen, string what) => $"  {pen.Paint("[x]", Green)} {what}";

    /// <summary>An open line of a checklist: what is missing, and how to get it.</summary>
    public static string Unticked(Pen pen, string what, string how) => $"  {pen.Paint("[ ]", Yellow)} {what,-WhatWidth} {how}";

    private static IEnumerable<string> Rows(string picture) =>
        picture.ReplaceLineEndings("\n").Split('\n').Where(row => row.Trim().Length > 0);

    private const int WhatWidth = 36;
}
