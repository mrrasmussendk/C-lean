namespace Fixture;

public static class Code
{
    public static void Charge(Order order)
    {
        order.Customer.Wallet.Pay();
    }
}

public class Order
{
    public Customer Customer = new Customer();
}

public class Customer
{
    public Wallet Wallet = new Wallet();

    public void Notify()
    {
    }
}

public class Wallet
{
    public void Pay()
    {
    }
}
