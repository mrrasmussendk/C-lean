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
        converter.Convert(2);
        rates.Verify(r => r.Current(), Times.Once());
    }
}
