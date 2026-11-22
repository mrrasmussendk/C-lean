namespace Fixture;

public static class Clock
{
    public static long Ticks()
    {
        return System.DateTime.UtcNow.Ticks;
    }
}
