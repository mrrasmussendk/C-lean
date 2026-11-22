using Xunit;

namespace Fixture;

public class CalculatorTests
{
    [Fact]
    public void Adds()
    {
        Assert.Equal(3, Calculator.Add(1, 2));
        Assert.Equal(0, Calculator.Add(0, 0));
        Assert.True(Calculator.Add(1, 1) > 1);
    }
}
