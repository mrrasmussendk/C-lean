using Xunit;

namespace Fixture;

public class CalculatorTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public void Adds(int a, int b)
    {
        var sum = Calculator.Add(a, b);
    }
}
