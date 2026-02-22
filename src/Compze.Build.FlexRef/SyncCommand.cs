namespace Compze.Build.FlexRef;

static class SyncCommand
{
    public static int Execute(DirectoryInfo rootDirectory)
    {
        Console.WriteLine($"Syncing FlexRef in: {rootDirectory.FullName}");

        if(!FlexRefConfigurationFile.ExistsIn(rootDirectory))
        {
            Console.Error.WriteLine($"Error: FlexRef.config.xml not found in {rootDirectory.FullName}.");
            Console.Error.WriteLine("Run 'flexref init' first to create the configuration.");
            return 1;
        }

        Console.WriteLine("Scanning projects...");
        var workspace = FlexRefWorkspace.ScanAndResolve(rootDirectory);
        Console.WriteLine($"  Resolved {workspace.FlexReferencedProjects.Count} flex-referenced project(s):");
        foreach(var flexReferencedProject in workspace.FlexReferencedProjects)
            Console.WriteLine($"    - {flexReferencedProject.PackageId} ({flexReferencedProject.CsprojFile.Name})");

        Console.WriteLine();
        Console.WriteLine("Writing FlexRef.props...");
        workspace.WriteFlexRefProps();

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
