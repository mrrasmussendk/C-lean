namespace Fixture;

public class Code
{
    private readonly int _count;

    public Code(int count)
    {
        _count = count;
    }

    public int Count()
    {
        return _count;
    }
}
