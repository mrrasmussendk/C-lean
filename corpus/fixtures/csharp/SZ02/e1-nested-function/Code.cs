using System;

namespace Fixture;

public static class Code
{
    public static int Run(bool a, bool b, bool c, bool d, int x)
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
        Func<int, int> inner = value =>
        {
            if (value > 0)
            {
                value += 1;
            }
            if (value > 1)
            {
                value += 1;
            }
            if (value > 2)
            {
                value += 1;
            }
            if (value > 3)
            {
                value += 1;
            }
            return value;
        };
        total += inner(x);
        return total;
    }
}
