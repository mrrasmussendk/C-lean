namespace Fixture;

public class Code
{
    private bool _enabled;

    public void Configure(bool enabled)
    {
        _enabled = enabled;
        Apply(enabled);
    }

    public bool IsEnabled()
    {
        return _enabled;
    }

    private static void Apply(bool value)
    {
    }
}
