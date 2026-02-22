using Compze.Build.FlexRef.Domain;

namespace Compze.Build.FlexRef.CLI;

static class SyncCommand
{
    public static int Execute(FlexRefWorkspace workspace)
    {
        Console.WriteLine($"Syncing FlexRef in: {workspace.RootDirectory.FullName}");

        try
        {
            workspace.Sync();
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
