namespace Fixture;

public static class Code
{
    public static int Three()
    {
        return Sum(1, 1);
    }

    private static int Sum(int a, int b)
    {
        return a + b + a;
    }
}
