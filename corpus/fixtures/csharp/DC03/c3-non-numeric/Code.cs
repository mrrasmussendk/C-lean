namespace Fixture;

public static class Code
{
    public static string Label(bool on)
    {
        bool enabled = true;
        return enabled == on ? "on" : "off";
    }
}
