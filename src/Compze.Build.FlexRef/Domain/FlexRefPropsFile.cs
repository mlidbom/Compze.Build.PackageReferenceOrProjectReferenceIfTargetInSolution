using System.Reflection;

namespace Compze.Build.FlexRef.Domain;

static class FlexRefPropsFile
{
    static FileInfo GetPropsFile(FlexRefWorkspace workspace) =>
        new(Path.Combine(workspace.RootDirectory.FullName, DomainConstants.BuildDirectoryName, DomainConstants.PropsFileName));

    public static string GetMsBuildImportProjectValue() =>
        $"$(MSBuildThisFileDirectory){DomainConstants.BuildDirectoryName}\\{DomainConstants.PropsFileName}";

    public static void Write(FlexRefWorkspace workspace)
    {
        var targetFile = GetPropsFile(workspace);
        Directory.CreateDirectory(targetFile.DirectoryName!);

        using var resourceStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(DomainConstants.PropsFileName)
            ?? throw new InvalidOperationException(
                "Embedded FlexRef.props resource not found in CLI assembly. This is a bug — please report it.");

        using var fileStream = File.Create(targetFile.FullName);
        resourceStream.CopyTo(fileStream);

        Console.WriteLine($"  Wrote: {targetFile.FullName}");
    }
}
