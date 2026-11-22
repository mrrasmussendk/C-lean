using System.Linq;

namespace Fixture;

public static class Code
{
    public static int Run(int[][] rows)
    {
        var summaryyyyyyyyyyyyyyyy = string.Format("{0}:{1}", string.Join(",", rows.Select(row => string.Join("+", row.Where(cell => cell > 0)))), rows.Length);
        return rows.Length;
    }
}
