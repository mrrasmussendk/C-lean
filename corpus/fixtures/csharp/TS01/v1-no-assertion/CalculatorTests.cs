using Xunit;

namespace Fixture;

public class CalculatorTests
{
    [Fact]
    public void Adds()
    {
        var sum = Calculator.Add(1, 2);
    }
}
