using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

record SlnxSolutionInfo(
    string SlnxFullPath,
    List<string> ProjectFileNames);

static class SlnxFileParser
{
    static readonly string[] DirectoriesToSkip = ["bin", "obj", "node_modules", ".git", ".vs", ".idea"];

    public static List<SlnxSolutionInfo> FindAndParseAllSolutions(string rootDirectory)
    {
        var solutions = new List<SlnxSolutionInfo>();
        foreach (var slnxPath in FindSlnxFilesRecursively(rootDirectory))
        {
            var solution = ParseSingleSlnx(slnxPath);
            if (solution != null)
                solutions.Add(solution);
        }
        return solutions;
    }

    static IEnumerable<string> FindSlnxFilesRecursively(string directory)
    {
        foreach (var file in Directory.GetFiles(directory, "*.slnx"))
            yield return file;

        foreach (var subdirectory in Directory.GetDirectories(directory))
        {
            var directoryName = Path.GetFileName(subdirectory);
            if (DirectoriesToSkip.Contains(directoryName, StringComparer.OrdinalIgnoreCase))
                continue;

            foreach (var file in FindSlnxFilesRecursively(subdirectory))
                yield return file;
        }
    }

    static SlnxSolutionInfo? ParseSingleSlnx(string slnxPath)
    {
        try
        {
            var document = XDocument.Load(slnxPath);
            var projectFileNames = document.Descendants("Project")
                .Select(element => element.Attribute("Path")?.Value)
                .Where(path => path != null)
                .Select(path => Path.GetFileName(path!))
                .ToList();

            return new SlnxSolutionInfo(
                SlnxFullPath: Path.GetFullPath(slnxPath),
                ProjectFileNames: projectFileNames);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Warning: Could not parse {slnxPath}: {exception.Message}");
            return null;
        }
    }
}
