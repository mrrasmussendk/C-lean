namespace Fixture;

public static class Code
{
    public static int Add(int a, int b)
    {
        return Sum(a, b);
    }

    private static int Sum(int a, int b)
    {
        return a + b + a;
    }
}
