namespace Fixture;

public static class Code
{
    public static string Make(Builder builder)
    {
        return builder.WithA().WithB().WithC().Build();
    }
}

public class Builder
{
    private string _text = "";

    public Builder WithA()
    {
        return new Builder { _text = _text + "a" };
    }

    public Builder WithB()
    {
        return new Builder { _text = _text + "b" };
    }

    public Builder WithC()
    {
        return new Builder { _text = _text + "c" };
    }

    public string Build()
    {
        return _text;
    }
}
