namespace Fixture;

public static class Code
{
    public static void Reset(
        ref Settings settings)
    {
        settings = new Settings();
    }
}

public class Settings
{
    public int Level;
}
