using Compze.Build.FlexRef.Domain;

namespace Compze.Build.FlexRef.CLI;

static class SyncCommand
{
    public static int Execute(DirectoryInfo rootDirectory)
    {
        Console.WriteLine($"Syncing FlexRef in: {rootDirectory.FullName}");

        try
        {
            FlexRefWorkspace.Sync(rootDirectory);
        }
        catch(ConfigurationNotFoundException)
        {
            Console.Error.WriteLine("Error: FlexRef.config.xml not found.");
            Console.Error.WriteLine("Run 'flexref init' first to create the configuration.");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Sync complete.");
        return 0;
    }
}
