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
        var flexReferences = ManagedProject.ScanAndResolveFlexReferences(rootDirectory, configFile);
        Console.WriteLine($"  Resolved {flexReferences.Count} flex reference(s):");
        foreach(var package in flexReferences)
            Console.WriteLine($"    - {package.PackageId} ({package.CsprojFile.Name})");

        Console.WriteLine();
        Console.WriteLine("Writing FlexRef.props...");
        FlexRefPropsFileWriter.WriteToDirectory(rootDirectory);

        Console.WriteLine();
        Console.WriteLine("Updating Directory.Build.props...");
        DirectoryBuildPropsFileUpdater.UpdateOrCreate(rootDirectory);

        Console.WriteLine();
        Console.WriteLine("Updating .csproj files...");
        foreach(var project in ManagedProject.AllProjects)
            project.UpdateCsprojIfNeeded();

        Console.WriteLine();
        Console.WriteLine("Updating NCrunch solution files...");
        var solutions = SlnxFileParser.FindAndParseAllSolutions(rootDirectory);
        foreach(var solution in solutions)
            NCrunchSolutionFileUpdater.UpdateOrCreate(solution, flexReferences);

        Console.WriteLine();
        Console.WriteLine("Sync complete.");
        return 0;
    }
}
