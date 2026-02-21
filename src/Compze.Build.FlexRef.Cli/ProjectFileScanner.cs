using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

static class ProjectFileScanner
{
    static readonly string[] DirectoriesToSkip = ["bin", "obj", "node_modules", ".git", ".vs", ".idea"];

    public static List<DiscoveredProject> ScanAllProjects(string rootDirectory)
    {
        var projects = new List<DiscoveredProject>();
        foreach (var csprojPath in FindCsprojFilesRecursively(rootDirectory))
        {
            var project = ParseSingleCsproj(csprojPath);
            if (project != null)
                projects.Add(project);
        }
        return projects;
    }

    static IEnumerable<string> FindCsprojFilesRecursively(string directory)
    {
        foreach (var file in Directory.GetFiles(directory, "*.csproj"))
            yield return file;

        foreach (var subdirectory in Directory.GetDirectories(directory))
        {
            var directoryName = Path.GetFileName(subdirectory);
            if (DirectoriesToSkip.Contains(directoryName, StringComparer.OrdinalIgnoreCase))
                continue;

            foreach (var file in FindCsprojFilesRecursively(subdirectory))
                yield return file;
        }
    }

    static DiscoveredProject? ParseSingleCsproj(string csprojPath)
    {
        try
        {
            var document = XDocument.Load(csprojPath);
            var rootElement = document.Root;
            if (rootElement == null) return null;

            var explicitPackageId = rootElement.Descendants("PackageId").FirstOrDefault()?.Value;
            var isPackableValue = rootElement.Descendants("IsPackable").FirstOrDefault()?.Value;
            var isExplicitlyNotPackable = string.Equals(isPackableValue, "false", StringComparison.OrdinalIgnoreCase);
            var isExplicitlyPackable = string.Equals(isPackableValue, "true", StringComparison.OrdinalIgnoreCase);
            var isPackable = !isExplicitlyNotPackable && (explicitPackageId != null || isExplicitlyPackable);

            var effectivePackageId = explicitPackageId
                ?? (isPackable ? Path.GetFileNameWithoutExtension(csprojPath) : null);

            var projectReferences = rootElement.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(includePath => includePath != null)
                .Select(includePath => new DiscoveredProjectReference(includePath!, Path.GetFileName(includePath!)))
                .ToList();

            var packageReferences = rootElement.Descendants("PackageReference")
                .Select(element => (
                    Name: element.Attribute("Include")?.Value,
                    Version: element.Attribute("Version")?.Value ?? ""))
                .Where(pair => pair.Name != null)
                .Select(pair => new DiscoveredPackageReference(pair.Name!, pair.Version))
                .ToList();

            return new DiscoveredProject(
                CsprojFullPath: Path.GetFullPath(csprojPath),
                CsprojFileName: Path.GetFileName(csprojPath),
                PackageId: effectivePackageId,
                IsPackable: isPackable,
                ProjectReferences: projectReferences,
                PackageReferences: packageReferences);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Warning: Could not parse {csprojPath}: {exception.Message}");
            return null;
        }
    }
}
