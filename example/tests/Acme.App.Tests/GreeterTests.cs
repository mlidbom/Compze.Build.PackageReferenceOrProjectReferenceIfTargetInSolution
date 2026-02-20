using Acme.App;
using Xunit;

namespace Acme.App.Tests;

public class GreeterTests
{
    [Fact]
    public void Greet_ReturnsBoldGreeting()
    {
        var result = Greeter.Greet("World");

        Assert.Equal("**Hello, World!**", result);
    }
}
