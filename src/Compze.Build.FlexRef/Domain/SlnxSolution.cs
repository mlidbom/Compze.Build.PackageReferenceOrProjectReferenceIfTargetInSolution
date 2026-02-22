using System.Xml.Linq;
using Compze.Build.FlexRef.SystemCE.IOCE;

namespace Compze.Build.FlexRef.Domain;

class SlnxSolution
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

    SlnxSolution(FileInfo slnxFile, FlexRefWorkspace workspace)
    {
        SlnxFile = slnxFile;
        Workspace = workspace;

        var document = XDocument.Load(slnxFile.FullName);
        ProjectFileNames = document.Descendants("Project")
                                   .Select(element => element.Attribute("Path")?.Value)
                                   .Where(path => path != null)
                                   .Select(path => Path.GetFileName(path!))
                                   .ToList();
    }

    public static List<SlnxSolution> FindAndParseAllSolutions(FlexRefWorkspace workspace) =>
        workspace.RootDirectory
           .EnumerateFiles(DomainConstants.SlnxSearchPattern, SearchOption.AllDirectories)
           .Where(file => !DomainConstants.DirectoriesToSkip.Any(file.HasDirectoryInPath))
           .Select(slnxFile => new SlnxSolution(slnxFile, workspace))
           .ToList();

    internal FlexRefWorkspace Workspace { get; }

    public List<FlexReferencedProject> AbsentFlexReferencedProjects =>
        Workspace.FlexReferencedProjects
                 .Where(flexReferencedProject => !ProjectFileNames
                                                    .Contains(flexReferencedProject.CsprojFile.Name, StringComparer.OrdinalIgnoreCase))
                 .OrderBy(flexReferencedProject => flexReferencedProject.PackageId, StringComparer.OrdinalIgnoreCase)
                 .ToList();

    public void UpdateNCrunchFile() => new NCrunchSolution(this).UpdateOrCreate();
}