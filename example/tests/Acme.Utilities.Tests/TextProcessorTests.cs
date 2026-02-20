using Acme.Utilities;
using Xunit;

namespace Acme.Utilities.Tests;

public class TextProcessorTests
{
    [Fact]
    public void MakeBold_WrapsInDoubleAsterisks()
    {
        var result = TextProcessor.MakeBold("hello");

        Assert.Equal("**hello**", result);
    }

    [Fact]
    public void MakeHeading_PrependsHashPrefix()
    {
        var result = TextProcessor.MakeHeading("Title");

        Assert.Equal("# Title", result);
    }
}
