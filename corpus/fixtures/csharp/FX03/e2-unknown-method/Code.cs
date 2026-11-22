namespace Fixture;

public static class Code
{
    public static void Refresh(Order order)
    {
        order.Touch();
    }
}

public class Order
{
    public int Total;

    public void Touch()
    {
    }
}
