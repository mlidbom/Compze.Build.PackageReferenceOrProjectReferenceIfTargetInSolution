using Microsoft.Build.Evaluation;

namespace Compze.Build.FlexRef.Cli;

static class ProjectFileScanner
{
    static readonly string[] DirectoriesToSkip = ["bin", "obj", "node_modules", ".git", ".vs", ".idea"];

    public static List<DiscoveredProject> ScanAllProjects(DirectoryInfo rootDirectory)
    {
        var projectCollection = new ProjectCollection();
        try
        {
            return FindCsprojFilesRecursively(rootDirectory)
                .Select(csprojFile => ParseSingleCsproj(csprojFile, projectCollection))
                .OfType<DiscoveredProject>()
                .ToList();
        }
        finally
        {
            projectCollection.UnloadAllProjects();
        }
    }

    static IEnumerable<FileInfo> FindCsprojFilesRecursively(DirectoryInfo directory)
    {
        foreach (var file in directory.GetFiles("*.csproj"))
            yield return file;

        foreach (var subdirectory in directory.GetDirectories())
        {
            if (DirectoriesToSkip.Contains(subdirectory.Name, StringComparer.OrdinalIgnoreCase))
                continue;

            foreach (var file in FindCsprojFilesRecursively(subdirectory))
                yield return file;
        }
    }

    static DiscoveredProject? ParseSingleCsproj(FileInfo csprojFile, ProjectCollection projectCollection)
    {
        try
        {
            var msbuildProject = new Project(csprojFile.FullName, null, null, projectCollection);

            var packageId = msbuildProject.GetNonEmptyPropertyOrNull("PackageId");
            var isPackableValue = msbuildProject.GetNonEmptyPropertyOrNull("IsPackable");
            var isExplicitlyNotPackable = string.Equals(isPackableValue, "false", StringComparison.OrdinalIgnoreCase);
            var isExplicitlyPackable = string.Equals(isPackableValue, "true", StringComparison.OrdinalIgnoreCase);
            var isPackable = !isExplicitlyNotPackable && (packageId != null || isExplicitlyPackable);

            var effectivePackageId = packageId
                ?? (isPackable ? Path.GetFileNameWithoutExtension(csprojFile.Name) : null);

            var projectReferences = msbuildProject.GetItems("ProjectReference")
                .Select(item => item.EvaluatedInclude)
                .Where(includePath => !string.IsNullOrEmpty(includePath))
                .Select(includePath => new DiscoveredProjectReference(includePath, Path.GetFileName(includePath)))
                .ToList();

            var packageReferences = msbuildProject.GetItems("PackageReference")
                .Select(item => (Name: item.EvaluatedInclude, Version: item.GetMetadataValue("Version")))
                .Where(pair => !string.IsNullOrEmpty(pair.Name))
                .Select(pair => new DiscoveredPackageReference(pair.Name, pair.Version))
                .ToList();

            return new DiscoveredProject(
                CsprojFullPath: csprojFile.FullName,
                CsprojFileName: csprojFile.Name,
                PackageId: effectivePackageId,
                IsPackable: isPackable,
                ProjectReferences: projectReferences,
                PackageReferences: packageReferences);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Warning: Could not parse {csprojFile.FullName}: {exception.Message}");
            return null;
        }
    }
}
