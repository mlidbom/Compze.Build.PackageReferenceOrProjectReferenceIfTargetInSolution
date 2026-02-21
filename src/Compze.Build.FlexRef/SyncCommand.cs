namespace Compze.Build.FlexRef;

static class SyncCommand
{
    public static int Execute(DirectoryInfo rootDirectory)
    {
        Console.WriteLine($"Syncing FlexRef in: {rootDirectory.FullName}");

        var configFile = new FlexRefConfigurationFile(rootDirectory);

        if(!configFile.Exists())
        {
            Console.Error.WriteLine($"Error: {configFile.ConfigFile.FullName} not found.");
            Console.Error.WriteLine("Run 'flexref init' first to create the configuration.");
            return 1;
        }

        configFile.Load();

        Console.WriteLine("Scanning projects...");
        var workspace = FlexRefWorkspace.ScanAndResolve(rootDirectory, configFile);
        Console.WriteLine($"  Resolved {workspace.FlexReferences.Count} flex reference(s):");
        foreach(var package in workspace.FlexReferences)
            Console.WriteLine($"    - {package.PackageId} ({package.CsprojFile.Name})");

        Console.WriteLine();
        Console.WriteLine("Writing FlexRef.props...");
        FlexRefPropsFileWriter.WriteToDirectory(rootDirectory);

        Console.WriteLine();
        Console.WriteLine("Updating Directory.Build.props...");
        workspace.UpdateDirectoryBuildProps();

        Console.WriteLine();
        Console.WriteLine("Updating .csproj files...");
        workspace.UpdateCsprojFiles();

        Console.WriteLine();
        Console.WriteLine("Updating NCrunch solution files...");
        workspace.UpdateNCrunchFiles();

        Console.WriteLine();
        Console.WriteLine("Sync complete.");
        return 0;
    }
}
