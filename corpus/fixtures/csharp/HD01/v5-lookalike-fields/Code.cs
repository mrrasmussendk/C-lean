namespace Fixture;

public static class Code
{
    // gate:allow SZ04 noreason="legacy signature" notexpires=2099-01-01 notapprover=marc
    public static int Sum(int a, int b, int c, int d, int e)
    {
        return a + b + c + d + e;
    }
}
