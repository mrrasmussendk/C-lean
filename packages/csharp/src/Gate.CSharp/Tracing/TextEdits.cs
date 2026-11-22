using System.Collections.Generic;
using System.Linq;

namespace Gate.CSharp.Tracing;

/// <summary>Replaces the characters from Start to End (exclusive) with Text; an insertion has Start equal to
/// End.</summary>
internal sealed record Edit(int Start, int End, string Text);

internal static class TextEdits
{
    public static Edit Insert(int position, string text) => new(position, position, text);

    /// <summary>Applies edits that do not overlap. Edits at one position are applied in the order given.</summary>
    public static string Apply(string text, IEnumerable<Edit> edits)
    {
        var ordered = edits.Select((edit, index) => (Edit: edit, Index: index))
            .OrderBy(item => item.Edit.Start).ThenBy(item => item.Index).Select(item => item.Edit).ToList();
        var kept = ordered.Select(
            (edit, index) => text[(index == 0 ? 0 : ordered[index - 1].End)..edit.Start] + edit.Text);
        var tail = text[ordered.Select(edit => edit.End).LastOrDefault()..];
        return string.Concat(kept) + tail;
    }
}
