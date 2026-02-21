using Microsoft.Build.Evaluation;

namespace Compze.Build.FlexRef.Cli;

static class ProjectExtensions
{
    public static string? GetNonEmptyPropertyOrNull(this Project project, string propertyName)
    {
        var value = project.GetPropertyValue(propertyName);
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
