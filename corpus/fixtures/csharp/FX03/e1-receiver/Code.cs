namespace Fixture;

public class Code
{
    private int _count;

    public void Bump()
    {
        this._count = this._count + 1;
    }

    public int Count()
    {
        return _count;
    }
}
