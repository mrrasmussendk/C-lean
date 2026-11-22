namespace Fixture;

public class Code
{
    private int _count;

    public int Next()
    {
        _count++;
        return _count;
    }
}
