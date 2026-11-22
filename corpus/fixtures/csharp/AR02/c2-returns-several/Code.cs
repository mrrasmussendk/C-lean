namespace Fixture;

public static class Code
{
    public static (int Sum, int Product) Combine(int a, int b)
    {
        return (a + b, a * b);
    }
}
