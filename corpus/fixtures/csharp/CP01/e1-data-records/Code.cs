namespace Fixture;

public static class Code
{
    public static int Size(Order order)
    {
        return order.Customer.Address.City.Name.Length;
    }
}

public record Order(Customer Customer);

public record Customer(Address Address);

public record Address(City City);

public record City(string Name);
