namespace Acme.Core;

public static class StringFormatter
{
    public static string Wrap(string text, string prefix, string suffix)
        => $"{prefix}{text}{suffix}";
}
