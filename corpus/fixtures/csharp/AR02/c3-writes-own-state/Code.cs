namespace Fixture;

public class Code
{
    private int _total;

    public void Add(int amount)
    {
        _total = _total + amount;
    }

    public int Total()
    {
        return _total;
    }
}
