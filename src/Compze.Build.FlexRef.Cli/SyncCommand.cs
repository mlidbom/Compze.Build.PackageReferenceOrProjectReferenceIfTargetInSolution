namespace Compze.Build.FlexRef.Cli;

static class SyncCommand
{
    public static int Execute(DirectoryInfo rootDirectory)
    {
        Console.WriteLine($"Syncing FlexRef in: {rootDirectory.FullName}");

        var configFile = new FlexRefConfigurationFile(rootDirectory);

        if(!configFile.Exists())
        {
            Console.Error.WriteLine($"Error: {configFile.ConfigFilePath} not found.");
            Console.Error.WriteLine("Run 'flexref init' first to create the configuration.");
            return 1;
        }

        configFile.Load();

        Console.WriteLine("Scanning projects...");
        var allProjects = ProjectFileScanner.ScanAllProjects(rootDirectory);

        var flexReferences = ResolveFlexReferences(configFile, allProjects);
        Console.WriteLine($"  Resolved {flexReferences.Count} flex reference(s):");
        foreach(var package in flexReferences)
            Console.WriteLine($"    - {package.PackageId} ({package.CsprojFile.Name})");

        Console.WriteLine();
        Console.WriteLine("Writing FlexRef.props...");
        FlexRefPropsFileWriter.WriteToDirectory(rootDirectory);

        Console.WriteLine();
        Console.WriteLine("Updating Directory.Build.props...");
        DirectoryBuildPropsFileUpdater.UpdateOrCreate(rootDirectory, flexReferences);

        Console.WriteLine();
        Console.WriteLine("Updating .csproj files...");
        foreach(var project in allProjects)
            CsprojFileUpdater.UpdateIfNeeded(project, flexReferences);

        Console.WriteLine();
        Console.WriteLine("Updating NCrunch solution files...");
        var solutions = SlnxFileParser.FindAndParseAllSolutions(rootDirectory);
        foreach(var solution in solutions)
            NCrunchSolutionFileUpdater.UpdateOrCreate(solution, flexReferences);

        Console.WriteLine();
        Console.WriteLine("Sync complete.");
        return 0;
    }

    static List<FlexReference> ResolveFlexReferences(FlexRefConfigurationFile configuration, List<DiscoveredProject> allProjects)
    {
        var packableProjects = allProjects
                              .Where(project => project.IsPackable && project.PackageId != null)
                              .ToList();

        var resolvedPackages = new List<FlexReference>();

        if(configuration.UseAutoDiscover)
        {
            foreach(var project in packableProjects)
            {
                if(configuration.AutoDiscoverExclusions
                                .Any(exclusion => exclusion.Equals(project.PackageId!, StringComparison.OrdinalIgnoreCase)))
                    continue;

                resolvedPackages.Add(new FlexReference(
                                         PackageId: project.PackageId!,
                                         CsprojFile: project.CsprojFile));
            }
        }

        foreach(var explicitPackageName in configuration.ExplicitPackageNames)
        {
            if(resolvedPackages.Any(existing =>
                                        existing.PackageId.Equals(explicitPackageName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var matchingProject = packableProjects
               .FirstOrDefault(project =>
                                   project.PackageId!.Equals(explicitPackageName, StringComparison.OrdinalIgnoreCase));

            if(matchingProject != null)
            {
                resolvedPackages.Add(new FlexReference(
                                         PackageId: matchingProject.PackageId!,
                                         CsprojFile: matchingProject.CsprojFile));
            } else
            {
                Console.Error.WriteLine(
                    $"  Warning: Explicit package '{explicitPackageName}' was not found in any project.");
            }
        }

        foreach(var package in resolvedPackages)
        {
            var expectedFileName = package.PackageId + ".csproj";
            if(!package.CsprojFile.Name.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(
                    $"  Warning: Package '{package.PackageId}' is in project file '{package.CsprojFile.Name}' (expected '{expectedFileName}')");
            }
        }

        return resolvedPackages
              .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
              .ToList();
    }
}
