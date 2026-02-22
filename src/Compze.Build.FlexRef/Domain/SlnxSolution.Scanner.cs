using System.Xml.Linq;

namespace Compze.Build.FlexRef.Domain;

partial class SlnxSolution
{
    static class Scanner
    {
        public static List<SlnxSolution> FindAndParseAll(DirectoryInfo rootDirectory) =>
            FindSlnxFilesRecursively(rootDirectory)
               .Select(ParseSlnx)
               .OfType<SlnxSolution>()
               .ToList();

        static IEnumerable<FileInfo> FindSlnxFilesRecursively(DirectoryInfo directory)
        {
            foreach(var file in directory.GetFiles(DomainConstants.SlnxSearchPattern))
                yield return file;

            foreach(var subdirectory in directory.GetDirectories())
            {
                if(ScannerDefaults.DirectoriesToSkip.Contains(subdirectory.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                foreach(var file in FindSlnxFilesRecursively(subdirectory))
                    yield return file;
            }
        }

        static SlnxSolution? ParseSlnx(FileInfo slnxFile)
        {
            try
            {
                var document = XDocument.Load(slnxFile.FullName);
                var projectFileNames = document.Descendants("Project")
                                               .Select(element => element.Attribute("Path")?.Value)
                                               .Where(path => path != null)
                                               .Select(path => Path.GetFileName(path!))
                                               .ToList();

                return new SlnxSolution(slnxFile: slnxFile, projectFileNames: projectFileNames);
            }
            catch(Exception exception)
            {
                Console.Error.WriteLine($"Warning: Could not parse {slnxFile.FullName}: {exception.Message}");
                return null;
            }
        }
    }
}
