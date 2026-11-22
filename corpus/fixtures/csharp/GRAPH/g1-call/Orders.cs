namespace Fixture;

public static class Orders
{
    public static long Stamp()
    {
        return Clock.Ticks();
    }
}
