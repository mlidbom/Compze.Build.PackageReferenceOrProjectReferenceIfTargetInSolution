namespace Compze.Build.FlexRef.Cli;

static class InitCommand
{
    public static int Execute(DirectoryInfo rootDirectory)
    {
        Console.WriteLine($"Initializing FlexRef in: {rootDirectory.FullName}");

        var configFile = new FlexRefConfigurationFile(rootDirectory);

        if (configFile.Exists())
        {
            Console.Error.WriteLine($"Error: {configFile.ConfigFilePath} already exists.");
            Console.Error.WriteLine("Delete it first if you want to re-initialize.");
            return 1;
        }

        Console.WriteLine("Scanning for packable projects...");
        var allProjects = ProjectFileScanner.ScanAllProjects(rootDirectory);
        var packableProjects = allProjects
            .Where(project => project.IsPackable && project.PackageId != null)
            .OrderBy(project => project.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine($"  Found {packableProjects.Count} packable project(s):");
        foreach (var project in packableProjects)
        {
            Console.WriteLine($"    - {project.PackageId} ({project.CsprojFileName})");

            var expectedFileName = project.PackageId + ".csproj";
            if (!project.CsprojFileName.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase))
                Console.Error.WriteLine($"      Warning: Package ID '{project.PackageId}' does not match file name '{project.CsprojFileName}'");
        }

        var packageIds = packableProjects.Select(project => project.PackageId!).ToList();
        configFile.CreateDefaultConfigFile(packageIds);
        Console.WriteLine($"  Created: {configFile.ConfigFilePath}");

        FlexRefPropsFileWriter.WriteToDirectory(rootDirectory);

        Console.WriteLine();
        Console.WriteLine("Initialization complete.");
        Console.WriteLine("Review FlexRef.config.xml, then run 'flexref sync' to generate the boilerplate.");
        return 0;
    }
}
