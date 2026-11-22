namespace Fixture;

public static class Code
{
    public static int Speed(Mode mode)
    {
        switch (mode)
        {
            case Mode.Fast:
                return 2;
            default:
                return 1;
        }
    }

    public static int Cost(Mode mode)
    {
        if (mode == Mode.Fast)
        {
            return 1;
        }
        return 0;
    }

    public static int Noise(Mode current)
    {
        if (current != Mode.Slow)
        {
            return 1;
        }
        return 0;
    }
}

public enum Mode
{
    Slow,
    Fast,
}
