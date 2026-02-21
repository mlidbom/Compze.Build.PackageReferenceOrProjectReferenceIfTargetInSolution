using System.Reflection;

namespace Compze.Build.FlexRef.Cli;

static class FlexRefPropsFileWriter
{
    const string BuildDirectoryName = "build";
    const string PropsFileName = "FlexRef.props";

    public static string GetPropsFilePath(string rootDirectory) =>
        Path.Combine(rootDirectory, BuildDirectoryName, PropsFileName);

    public static string GetMsBuildImportProjectValue() =>
        $"$(MSBuildThisFileDirectory){BuildDirectoryName}\\{PropsFileName}";

    public static void WriteToDirectory(string rootDirectory)
    {
        var targetPath = GetPropsFilePath(rootDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        using var resourceStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("FlexRef.props")
            ?? throw new InvalidOperationException(
                "Embedded FlexRef.props resource not found in CLI assembly. This is a bug — please report it.");

        using var fileStream = File.Create(targetPath);
        resourceStream.CopyTo(fileStream);

        Console.WriteLine($"  Wrote: {targetPath}");
    }
}
