using Acme.Core;

namespace Acme.Utilities;

public static class TextProcessor
{
    public static string MakeBold(string text)
        => StringFormatter.Wrap(text, "**", "**");

    public static string MakeHeading(string text)
        => StringFormatter.Wrap(text, "# ", "");
}
