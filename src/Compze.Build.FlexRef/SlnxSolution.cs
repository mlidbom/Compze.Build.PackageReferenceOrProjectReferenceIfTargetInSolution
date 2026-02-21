namespace Compze.Build.FlexRef;

partial class SlnxSolution
{
    public FileInfo SlnxFile { get; }
    public List<string> ProjectFileNames { get; }

    public FileInfo NCrunchFile
    {
        get
        {
            var solutionStem = Path.GetFileNameWithoutExtension(SlnxFile.Name);
            return new FileInfo(Path.Combine(SlnxFile.DirectoryName!, solutionStem + ".v3.ncrunchsolution"));
        }
    }

    SlnxSolution(FileInfo slnxFile, List<string> projectFileNames)
    {
        SlnxFile = slnxFile;
        ProjectFileNames = projectFileNames;
    }

    public static List<SlnxSolution> FindAndParseAllSolutions(DirectoryInfo rootDirectory) =>
        Scanner.FindAndParseAll(rootDirectory);

    public List<FlexReference> FindAbsentFlexReferences(IReadOnlyList<FlexReference> flexReferences) =>
        flexReferences
            .Where(package => !ProjectFileNames
                                   .Contains(package.CsprojFile.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
