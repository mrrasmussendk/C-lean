using System.Linq;

namespace Fixture;

public static class Code
{
    public static int Run(int[][] rows, int x)
    {
        int total = 0;
        if (x > 0)
        {
            total += 1;
        }
        if (x > 1)
        {
            total += 1;
        }
        if (x > 2)
        {
            total += 1;
        }
        if (x > 3)
        {
            total += 1;
        }
        if (x > 4)
        {
            total += 1;
        }
        if (x > 5)
        {
            total += 1;
        }
        if (x > 6)
        {
            total += 1;
        }
        if (x > 7)
        {
            total += 1;
        }
        if (x > 8)
        {
            total += 1;
        }
        if (x > 9)
        {
            total += 1;
        }
        if (x > 10)
        {
            total += 1;
        }
        if (x > 11)
        {
            total += 1;
        }
        var pairs =
            from a in rows
            from b in a
            where b > 0
            where b < x
            select b;
        total += pairs.Count();
        return total;
    }
}
