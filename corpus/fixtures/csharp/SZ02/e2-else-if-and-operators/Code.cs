namespace Fixture;

public static class Code
{
    public static int Run(bool a, bool b, bool c, bool d, int x)
    {
        int total = 0;
        if (a && b && c)
        {
            total += 1;
        }
        else if (a || b || c || d)
        {
            total += 1;
        }
        else if (c)
        {
            total += 1;
        }
        else if (d)
        {
            total += 1;
        }
        else
        {
            total += 1;
        }
        if (a && b)
        {
            total += 1;
        }
        else if (a || b)
        {
            total += 1;
        }
        else if (c)
        {
            total += 1;
        }
        else if (d)
        {
            total += 1;
        }
        else
        {
            total += 1;
        }
        if (x > 0)
        {
            total += 1;
        }
        return total;
    }
}
