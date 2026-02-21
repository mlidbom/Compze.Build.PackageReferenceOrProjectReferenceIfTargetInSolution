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

        configFile.CreateDefault();

        FlexRefPropsFileWriter.WriteToDirectory(rootDirectory);

        Console.WriteLine();
        Console.WriteLine("Initialization complete.");
        Console.WriteLine("Review FlexRef.config.xml, then run 'flexref sync' to generate the boilerplate.");
        return 0;
    }
}
