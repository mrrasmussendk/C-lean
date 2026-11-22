namespace Fixture;

public static class Code
{
    public static int Rows()
    {
        var pinRows = new[]
        {
            new[]
            {
                7,
                8,
                9,
            },
            new[] { 1 },
        };
        return pinRows.Length;
    }
}
