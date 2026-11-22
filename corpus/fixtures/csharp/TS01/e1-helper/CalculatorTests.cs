using Xunit;

namespace Fixture;

public class CalculatorTests
{
    [Fact]
    public void Adds()
    {
        AssertPositive(Calculator.Add(1, 2));
    }

    private static void AssertPositive(int value)
    {
        Assert.True(value > 0);
    }
}
