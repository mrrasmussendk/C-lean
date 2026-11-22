namespace Fixture;

public static class Code
{
    public static string Label(
        bool verbose)
    {
        return !verbose ? "short" : "long text";
    }
}
