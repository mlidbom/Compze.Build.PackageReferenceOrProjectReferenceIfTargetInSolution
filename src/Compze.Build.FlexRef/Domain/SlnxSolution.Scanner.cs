using System.Xml.Linq;

namespace Compze.Build.FlexRef.Domain;

partial class SlnxSolution
{
    static class Scanner
    {
        public static List<SlnxSolution> FindAndParseAll(FlexRefWorkspace workspace) =>
            FindSlnxFilesRecursively(workspace.RootDirectory)
               .Select(slnxFile => ParseSlnx(slnxFile, workspace))
               .OfType<SlnxSolution>()
               .ToList();

        static IEnumerable<FileInfo> FindSlnxFilesRecursively(DirectoryInfo directory)
        {
            foreach(var file in directory.GetFiles(DomainConstants.SlnxSearchPattern))
                yield return file;

            foreach(var subdirectory in directory.GetDirectories())
            {
                if(DomainConstants.DirectoriesToSkip.Contains(subdirectory.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                foreach(var file in FindSlnxFilesRecursively(subdirectory))
                    yield return file;
            }
        }

        static SlnxSolution? ParseSlnx(FileInfo slnxFile, FlexRefWorkspace workspace)
        {
            try
            {
                var document = XDocument.Load(slnxFile.FullName);
                var projectFileNames = document.Descendants("Project")
                                               .Select(element => element.Attribute("Path")?.Value)
                                               .Where(path => path != null)
                                               .Select(path => Path.GetFileName(path!))
                                               .ToList();

                return new SlnxSolution(slnxFile: slnxFile, projectFileNames: projectFileNames, workspace: workspace);
            }
            catch(Exception exception)
            {
                Console.Error.WriteLine($"Warning: Could not parse {slnxFile.FullName}: {exception.Message}");
                return null;
            }
        }
    }
}
