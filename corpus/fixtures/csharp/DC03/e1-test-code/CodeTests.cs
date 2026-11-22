using Xunit;

namespace Fixture;

public class CodeTests
{
    [Fact]
    public void Same()
    {
        Assert.Equal(42, Code.Same(42));
    }
}
