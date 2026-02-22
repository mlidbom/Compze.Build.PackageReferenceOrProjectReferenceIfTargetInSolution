using Microsoft.Build.Evaluation;

namespace Compze.Build.FlexRef.Domain;

partial class ManagedProject
{
    static class Scanner
    {
        internal static List<ManagedProject> ScanDirectory(DirectoryInfo rootDirectory)
        {
            using var projectCollection = new ProjectCollection();
            return FindCsprojFilesRecursively(rootDirectory)
                .Select(csprojFile => ParseCsproj(csprojFile, projectCollection))
                .OfType<ManagedProject>()
                .ToList();
        }

        static IEnumerable<FileInfo> FindCsprojFilesRecursively(DirectoryInfo directory)
        {
            foreach(var file in directory.GetFiles(DomainConstants.CsprojSearchPattern))
                yield return file;

            foreach(var subdirectory in directory.GetDirectories())
            {
                if(DomainConstants.DirectoriesToSkip.Contains(subdirectory.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                foreach(var file in FindCsprojFilesRecursively(subdirectory))
                    yield return file;
            }
        }

        static ManagedProject? ParseCsproj(FileInfo csprojFile, ProjectCollection projectCollection)
        {
            try
            {
                return new ManagedProject(csprojFile, projectCollection);
            }
            catch(Exception exception)
            {
                Console.Error.WriteLine($"Warning: Could not parse {csprojFile.FullName}: {exception.Message}");
                return null;
            }
        }
    }
}
