using Acme.Core;
using Xunit;

namespace Acme.Core.Tests;

public class StringFormatterTests
{
    [Fact]
    public void Wrap_SurroundsTextWithPrefixAndSuffix()
    {
        var result = StringFormatter.Wrap("hello", "[", "]");

        Assert.Equal("[hello]", result);
    }
}
