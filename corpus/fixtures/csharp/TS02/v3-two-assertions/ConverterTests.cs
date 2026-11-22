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
        var other = new Mock<IRates>();
        other.Setup(r => r.Current()).Returns(7);
        Assert.Equal(5, rates.Object.Current());
        Assert.Equal(7, other.Object.Current());
    }
}
