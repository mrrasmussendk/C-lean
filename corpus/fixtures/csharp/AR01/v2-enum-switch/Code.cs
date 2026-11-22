namespace Fixture;

public static class Code
{
    public static int Speed(
        Mode mode)
    {
        switch (mode)
        {
            case Mode.Fast:
                return 2;
            default:
                return 1;
        }
    }
}

public enum Mode
{
    Slow,
    Fast,
}
