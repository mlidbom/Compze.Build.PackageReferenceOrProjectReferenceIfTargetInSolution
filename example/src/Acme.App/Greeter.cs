using Acme.Utilities;

namespace Acme.App;

public static class Greeter
{
    public static string Greet(string name)
        => TextProcessor.MakeBold($"Hello, {name}!");
}
