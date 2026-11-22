namespace Fixture;

public interface IRates
{
    int Current();
}

public class Converter
{
    private readonly IRates _rates;

    public Converter(IRates rates)
    {
        _rates = rates;
    }

    public int Convert(int amount)
    {
        return amount * _rates.Current();
    }
}
