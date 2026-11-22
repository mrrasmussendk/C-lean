namespace Fixture;

public static class Code
{
    public static void Price(Order order)
    {
        order.Total = 5;
    }
}

public class Order
{
    public int Total;

    public void Touch()
    {
    }
}
