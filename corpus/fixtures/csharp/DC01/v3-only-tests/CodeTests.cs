using Xunit;

namespace Fixture;

public class CodeTests
{
    [Fact]
    public void Discounts()
    {
        Assert.Equal(1, Code.Discount());
    }
}
