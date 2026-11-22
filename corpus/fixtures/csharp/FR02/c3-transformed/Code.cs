namespace Fixture;

public static class Code
{
    public static int Add(int a, string b)
    {
        return Sum(a + 1, b.Trim().Length);
    }

    private static int Sum(int a, int b)
    {
        return a + b + a;
    }
}
