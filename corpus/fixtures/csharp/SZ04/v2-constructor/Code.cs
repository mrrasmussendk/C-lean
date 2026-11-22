namespace Fixture;

public class Code
{
    private readonly int _sum;

    public Code(int a, int b, int c, int d, int e, int f)
    {
        _sum = a + b + c + d + e + f;
    }

    public int Sum()
    {
        return _sum;
    }
}
