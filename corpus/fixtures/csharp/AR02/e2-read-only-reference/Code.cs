namespace Fixture;

public static class Code
{
    public static int Sum(in Point point)
    {
        return point.X + point.Y;
    }
}

public struct Point
{
    public int X;
    public int Y;
}
