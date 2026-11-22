using Moq;
using Xunit;

namespace Fixture;

public class ConverterTests
{
    [Fact]
    public void Converts()
    {
        var sum = 2 + 3;
        Assert.Equal(5, sum);
    }
}
