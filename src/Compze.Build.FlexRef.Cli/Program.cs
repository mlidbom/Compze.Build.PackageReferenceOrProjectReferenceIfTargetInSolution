namespace Compze.Build.FlexRef.Cli;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
            return PrintUsageAndReturnError();

        var command = args[0].ToLowerInvariant();
        var rootDirectory = args.Length > 1 ? Path.GetFullPath(args[1]) : Directory.GetCurrentDirectory();

        if (!Directory.Exists(rootDirectory))
        {
            Console.Error.WriteLine($"Error: Directory not found: {rootDirectory}");
            return 1;
        }

        return command switch
        {
            "init" => InitCommand.Execute(rootDirectory),
            "sync" => SyncCommand.Execute(rootDirectory),
            _ => PrintUsageAndReturnError()
        };
    }

    static int PrintUsageAndReturnError()
    {
        Console.WriteLine("""
            Usage: flexref <command> [directory]

            Commands:
              init   Create FlexRef.config.xml and build/FlexRef.props
              sync   Update all managed files based on configuration

            If [directory] is omitted, the current directory is used.
            """);
        return 1;
    }
}
