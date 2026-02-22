using Microsoft.Build.Evaluation;

namespace Compze.Build.FlexRef.MicrosoftCE.BuildCE.EvaluationCE;

static class ProjectCE
{
    public static string? GetNonEmptyPropertyOrNull(this Project project, string propertyName)
    {
        var value = project.GetPropertyValue(propertyName);
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
