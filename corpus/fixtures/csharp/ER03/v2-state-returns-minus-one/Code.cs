namespace Fixture;

public class Code
{
    private int _top;

    public int Push(int value)
    {
        if (value < 0)
        {
            return -1;
        }
        _top = value;
        return _top;
    }
}
