namespace Fixture;

public class Code
{
    private string _name = "";

    public Code WithName(string name)
    {
        _name = name;
        return this;
    }

    public string Name()
    {
        return _name;
    }
}
