using Xunit;

namespace Fixture;

public class CalculatorTests
{
    [Fact]
    public void Adds()
    {
        System.Console.WriteLine(Calculator.Add(1, 2));
    }
}
