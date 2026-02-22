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

    SlnxSolution(FileInfo slnxFile, List<string> projectFileNames, FlexRefWorkspace workspace)
    {
        SlnxFile = slnxFile;
        ProjectFileNames = projectFileNames;
        Workspace = workspace;
    }

    public static List<SlnxSolution> FindAndParseAllSolutions(FlexRefWorkspace workspace) =>
        Scanner.FindAndParseAll(workspace);

    internal FlexRefWorkspace Workspace { get; }

    public List<FlexReferencedProject> AbsentFlexReferencedProjects =>
        Workspace.FlexReferencedProjects
            .Where(flexReferencedProject => !ProjectFileNames
                                   .Contains(flexReferencedProject.CsprojFile.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(flexReferencedProject => flexReferencedProject.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public void UpdateNCrunchFile() =>
        new NCrunchSolution(NCrunchFile, AbsentFlexReferencedProjects).UpdateOrCreate();
}
