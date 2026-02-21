namespace Compze.Build.FlexRef.Cli;

static class SyncCommand
{
    public static int Execute(string rootDirectory)
    {
        Console.WriteLine($"Syncing FlexRef in: {rootDirectory}");

        if (!FlexRefConfigurationFile.ExistsIn(rootDirectory))
        {
            Console.Error.WriteLine($"Error: {FlexRefConfigurationFile.GetConfigFilePath(rootDirectory)} not found.");
            Console.Error.WriteLine("Run 'flexref init' first to create the configuration.");
            return 1;
        }

        var configuration = FlexRefConfigurationFile.LoadFrom(rootDirectory);

        Console.WriteLine("Scanning projects...");
        var allProjects = ProjectFileScanner.ScanAllProjects(rootDirectory);

        var switchablePackages = ResolveSwitchablePackages(configuration, allProjects);
        Console.WriteLine($"  Resolved {switchablePackages.Count} switchable package(s):");
        foreach (var package in switchablePackages)
            Console.WriteLine($"    - {package.PackageId} ({package.CsprojFileName})");

        Console.WriteLine();
        Console.WriteLine("Writing FlexRef.props...");
        FlexRefPropsFileWriter.WriteToDirectory(rootDirectory);

        Console.WriteLine();
        Console.WriteLine("Updating Directory.Build.props...");
        DirectoryBuildPropsFileUpdater.UpdateOrCreate(rootDirectory, switchablePackages);

        Console.WriteLine();
        Console.WriteLine("Updating .csproj files...");
        foreach (var project in allProjects)
            CsprojFileUpdater.UpdateIfNeeded(project, switchablePackages);

        Console.WriteLine();
        Console.WriteLine("Updating NCrunch solution files...");
        var solutions = SlnxFileParser.FindAndParseAllSolutions(rootDirectory);
        foreach (var solution in solutions)
            NCrunchSolutionFileUpdater.UpdateOrCreate(solution, switchablePackages);

        Console.WriteLine();
        Console.WriteLine("Sync complete.");
        return 0;
    }

    static List<SwitchablePackageInfo> ResolveSwitchablePackages(
        FlexRefConfigurationFile configuration,
        List<DiscoveredProject> allProjects)
    {
        var packableProjects = allProjects
            .Where(project => project.IsPackable && project.PackageId != null)
            .ToList();

        var resolvedPackages = new List<SwitchablePackageInfo>();

        if (configuration.UseAutoDiscover)
        {
            foreach (var project in packableProjects)
            {
                if (configuration.AutoDiscoverExclusions
                    .Any(exclusion => exclusion.Equals(project.PackageId!, StringComparison.OrdinalIgnoreCase)))
                    continue;

                resolvedPackages.Add(new SwitchablePackageInfo(
                    PackageId: project.PackageId!,
                    CsprojFileName: project.CsprojFileName,
                    CsprojFullPath: project.CsprojFullPath));
            }
        }

        foreach (var explicitPackageName in configuration.ExplicitPackageNames)
        {
            if (resolvedPackages.Any(existing =>
                existing.PackageId.Equals(explicitPackageName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var matchingProject = packableProjects
                .FirstOrDefault(project =>
                    project.PackageId!.Equals(explicitPackageName, StringComparison.OrdinalIgnoreCase));

            if (matchingProject != null)
            {
                resolvedPackages.Add(new SwitchablePackageInfo(
                    PackageId: matchingProject.PackageId!,
                    CsprojFileName: matchingProject.CsprojFileName,
                    CsprojFullPath: matchingProject.CsprojFullPath));
            }
            else
            {
                Console.Error.WriteLine(
                    $"  Warning: Explicit package '{explicitPackageName}' was not found in any project.");
            }
        }

        foreach (var package in resolvedPackages)
        {
            var expectedFileName = package.PackageId + ".csproj";
            if (!package.CsprojFileName.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(
                    $"  Warning: Package '{package.PackageId}' is in project file '{package.CsprojFileName}' (expected '{expectedFileName}')");
            }
        }

        return resolvedPackages
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
