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
        var converter = new Converter(rates.Object);
        Assert.Equal(10, converter.Convert(2));
    }
}
