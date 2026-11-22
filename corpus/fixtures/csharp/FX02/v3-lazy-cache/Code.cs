namespace Fixture;

public class Code
{
    private string? _cache;

    public string Name
    {
        get
        {
            if (_cache == null)
            {
                _cache = "computed";
            }
            return _cache;
        }
    }
}
