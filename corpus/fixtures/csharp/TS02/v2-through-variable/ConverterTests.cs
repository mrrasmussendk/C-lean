using Moq;
using Xunit;

namespace Fixture;

public class ConverterTests
{
    [Fact]
    public void Converts()
    {
        var rates = new Mock<IRates>();
        rates.Setup(r => r.Current()).Returns(5);
        var current = rates.Object.Current();
        Assert.Equal(5, current);
    }
}
