namespace Compze.Build.FlexRef;

static class InitCommand
{
    public static int Execute(DirectoryInfo rootDirectory)
    {
        Console.WriteLine($"Initializing FlexRef in: {rootDirectory.FullName}");

        if(FlexRefConfigurationFile.ExistsIn(rootDirectory))
        {
            Console.Error.WriteLine($"Error: FlexRef.config.xml already exists in {rootDirectory.FullName}.");
            Console.Error.WriteLine("Delete it first if you want to re-initialize.");
            return 1;
        }

        Console.WriteLine("Scanning for packable projects...");
        var workspace = FlexRefWorkspace.Scan(rootDirectory);
        workspace.CreateDefaultConfiguration();
        workspace.WriteFlexRefProps();

        Console.WriteLine();
        Console.WriteLine("Initialization complete.");
        Console.WriteLine("Review FlexRef.config.xml, then run 'flexref sync' to generate the boilerplate.");
        return 0;
    }
}
