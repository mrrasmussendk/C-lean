namespace Fixture;

public static class Code
{
    public static int Add(int a, int b, int c, int d, int e)
    {
        return Total(a, b, c, d, 0);
    }

    private static int Total(int a, int b, int c, int d, int e)
    {
        return a + b + c + d + e + a;
    }
}
