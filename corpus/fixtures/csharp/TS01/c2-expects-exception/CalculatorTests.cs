using Xunit;

namespace Fixture;

public class CalculatorTests
{
    [Fact]
    public void Throws()
    {
        Assert.Throws<System.OverflowException>(() => checked(Calculator.Add(int.MaxValue, 1)));
    }
}
