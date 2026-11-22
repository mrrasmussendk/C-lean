namespace Fixture;

public static class Code
{
    private static int _last;

    public static int Record(int value)
    {
        _last = value;
        return _last * value;
    }
}
