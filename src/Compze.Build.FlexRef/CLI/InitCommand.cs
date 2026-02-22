using Compze.Build.FlexRef.Domain;

namespace Compze.Build.FlexRef.CLI;

static class InitCommand
{
    public static int Execute(DirectoryInfo rootDirectory)
    {
        Console.WriteLine($"Initializing FlexRef in: {rootDirectory.FullName}");

        try
        {
            FlexRefWorkspace.Init(rootDirectory);
        }
        catch(ConfigurationAlreadyExistsException)
        {
            Console.Error.WriteLine("Error: FlexRef.config.xml already exists.");
            Console.Error.WriteLine("Delete it first if you want to re-initialize.");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Initialization complete.");
        Console.WriteLine("Review FlexRef.config.xml, then run 'flexref sync' to generate the boilerplate.");
        return 0;
    }
}