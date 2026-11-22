namespace Fixture;

public static class Code
{
    public static int Price(
        int amount,
        bool discounted)
    {
        if (discounted)
        {
            return amount / 2;
        }
        return amount;
    }
}
