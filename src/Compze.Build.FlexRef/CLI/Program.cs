using Compze.Build.FlexRef.Domain;
using Microsoft.Build.Locator;

namespace Compze.Build.FlexRef.CLI;

static class Program
{
    static int Main(string[] args)
    {
        MSBuildLocator.RegisterDefaults();

        if (args.Length == 0)
            return PrintUsageAndReturnError();

        var command = args[0].ToLowerInvariant();
        var rootDirectoryPath = args.Length > 1 ? Path.GetFullPath(args[1]) : Directory.GetCurrentDirectory();
        var rootDirectory = new DirectoryInfo(rootDirectoryPath);

        FlexRefWorkspace workspace;
        try
        {
            workspace = new FlexRefWorkspace(rootDirectory);
        }
        catch(RootDirectoryNotFoundException)
        {
            Console.Error.WriteLine($"Error: Directory not found: {rootDirectory.FullName}");
            return 1;
        }

        return command switch
        {
            CliConstants.Commands.Init => InitCommand.Execute(workspace),
            CliConstants.Commands.Sync => SyncCommand.Execute(workspace),
            _ => PrintUsageAndReturnError()
        };
    }

    static int PrintUsageAndReturnError()
    {
        Console.WriteLine($"""
            Usage: {CliConstants.CommandName} <command> [directory]

            Commands:
              {CliConstants.Commands.Init}   Create {DomainConstants.ConfigurationFileName} and {DomainConstants.BuildDirectoryName}/{DomainConstants.PropsFileName}
              {CliConstants.Commands.Sync}   Update all managed files based on configuration

            If [directory] is omitted, the current directory is used.
            """);
        return 1;
    }
}
