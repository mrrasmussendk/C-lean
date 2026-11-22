namespace Fixture;

public static class Code
{
    private static readonly int[][] PinRows =
    {
        new[] { 7, 8, 9, 10 },
        new[] { 4, 5, 6 },
        new[] { 2, 3 },
        new[] { 1 },
    };

    public static int Rows()
    {
        return PinRows.Length;
    }
}
