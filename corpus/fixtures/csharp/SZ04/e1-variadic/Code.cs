namespace Fixture;

public static class Code
{
    public static int Sum(int a, int b, int c, params int[] rest)
    {
        return a + b + c + rest.Length;
    }
}
