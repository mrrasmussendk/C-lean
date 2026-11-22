namespace Fixture;

public static class Code
{
    public static int Run(bool a, bool b, bool c, bool d, int x)
    {
        int total = 0;
        if (a && b || c)
        {
            total += 1;
        }
        else if (a || b && c)
        {
            total += 1;
        }
        else if (b && c || d)
        {
            total += 1;
        }
        else if (b || c && d)
        {
            total += 1;
        }
        else if (a && d || x > 1)
        {
            total += 1;
        }
        else
        {
            total += 1;
        }
        return total;
    }
}
