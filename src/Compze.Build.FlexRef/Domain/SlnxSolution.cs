namespace Compze.Build.FlexRef.Domain;

partial class SlnxSolution
{
    public FileInfo SlnxFile { get; }
    public List<string> ProjectFileNames { get; }

    public FileInfo NCrunchFile
    {
        get
        {
            var solutionStem = Path.GetFileNameWithoutExtension(SlnxFile.Name);
            return new FileInfo(Path.Combine(SlnxFile.DirectoryName!, solutionStem + DomainConstants.NCrunchSolutionFileExtension));
        }
    }

    SlnxSolution(FileInfo slnxFile, List<string> projectFileNames)
    {
        SlnxFile = slnxFile;
        ProjectFileNames = projectFileNames;
    }

    public static List<SlnxSolution> FindAndParseAllSolutions(DirectoryInfo rootDirectory) =>
        Scanner.FindAndParseAll(rootDirectory);

    internal FlexRefWorkspace Workspace { get; set; } = null!;

    public List<FlexReferencedProject> AbsentFlexReferencedProjects =>
        Workspace.FlexReferencedProjects
            .Where(flexReferencedProject => !ProjectFileNames
                                   .Contains(flexReferencedProject.CsprojFile.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(flexReferencedProject => flexReferencedProject.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
