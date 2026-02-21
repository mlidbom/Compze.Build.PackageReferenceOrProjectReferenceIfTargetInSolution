namespace Compze.Build.FlexRef.Cli;

static class StringCE
{
    public static bool EqualsIgnoreCase(this string? @this, string? other) =>
        string.Equals(@this, other, StringComparison.OrdinalIgnoreCase);
}
