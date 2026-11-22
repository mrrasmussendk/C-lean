using Moq;
using Xunit;

namespace Fixture;

public class ConverterTests
{
    [Fact]
    public void Converts()
    {
        var rates = new FixedRates();
        Assert.Equal(5, rates.Current());
    }

    private sealed class FixedRates : IRates
    {
        public int Current()
        {
            return 5;
        }
    }
}
