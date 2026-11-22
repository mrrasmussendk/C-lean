using System.Linq;

namespace Fixture;

public static class Code
{
    public static int Run(int[] row, int x)
    {
        var kept =
            from b in row
            where b > x
            select b;
        return kept.Count();
    }
}
