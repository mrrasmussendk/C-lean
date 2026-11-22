namespace Fixture;

public static class Code
{
    public static string Run()
    {
        return Greet(null);
    }

    private static string Greet(string? name)
    {
        return "hello " + name;
    }
}
