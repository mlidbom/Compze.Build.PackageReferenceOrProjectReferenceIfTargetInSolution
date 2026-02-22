using Compze.Build.FlexRef.MicrosoftCE.BuildCE.EvaluationCE;
using Compze.Build.FlexRef.SystemCE;
using Microsoft.Build.Evaluation;

namespace Compze.Build.FlexRef.Domain;

record ProjectReferenceEntry(string IncludePath, string ResolvedFileName);

record PackageReferenceEntry(string PackageName, string Version);

partial class ManagedProject
{
    public FileInfo CsprojFile { get; }
    public string? PackageId { get; }
    public bool IsPackable { get; }
    public List<ProjectReferenceEntry> ProjectReferences { get; }
    public List<PackageReferenceEntry> PackageReferences { get; }

    ManagedProject(FileInfo csprojFile, ProjectCollection projectCollection)
    {
        CsprojFile = csprojFile;

        var msbuildProject = new Project(csprojFile.FullName, null, null, projectCollection);

        var explicitPackageId = msbuildProject.GetNonEmptyPropertyOrNull("PackageId");
        var isPackableValue = msbuildProject.GetNonEmptyPropertyOrNull("IsPackable");
        var isExplicitlyNotPackable = isPackableValue.EqualsIgnoreCase("false");
        var isExplicitlyPackable = isPackableValue.EqualsIgnoreCase("true");
        IsPackable = !isExplicitlyNotPackable && (explicitPackageId != null || isExplicitlyPackable);

        PackageId = explicitPackageId
            ?? (IsPackable ? Path.GetFileNameWithoutExtension(csprojFile.Name) : null);

        ProjectReferences = msbuildProject.GetItems("ProjectReference")
            .Select(item => item.EvaluatedInclude)
            .Where(includePath => !string.IsNullOrEmpty(includePath))
            .Select(includePath => new ProjectReferenceEntry(includePath, Path.GetFileName(includePath)))
            .ToList();

        PackageReferences = msbuildProject.GetItems("PackageReference")
            .Select(item => (Name: item.EvaluatedInclude, Version: item.GetMetadataValue("Version")))
            .Where(pair => !string.IsNullOrEmpty(pair.Name))
            .Select(pair => new PackageReferenceEntry(pair.Name, pair.Version))
            .ToList();
    }

    internal static List<ManagedProject> ScanDirectory(DirectoryInfo rootDirectory) =>
        Scanner.ScanDirectory(rootDirectory);

    internal static List<FlexReferencedProject> ResolveFlexReferencedProjects(FlexRefConfigurationFile configuration, List<ManagedProject> allProjects) =>
        FlexReferenceResolver.Resolve(configuration, allProjects);

    internal FlexRefWorkspace Workspace { get; set; } = null!;

    public List<FlexReferencedProject> FlexReferencedProjects
    {
        get
        {
            var result = new List<FlexReferencedProject>();

            foreach(var flexReferencedProject in Workspace.FlexReferencedProjects)
            {
                if(CsprojFile.FullName.EqualsIgnoreCase(flexReferencedProject.CsprojFile.FullName))
                    continue;

                var hasMatchingProjectReference = ProjectReferences
                                                         .Any(reference => reference.ResolvedFileName.EqualsIgnoreCase(flexReferencedProject.CsprojFile.Name));

                var hasMatchingPackageReference = PackageReferences
                                                         .Any(reference => reference.PackageName.EqualsIgnoreCase(flexReferencedProject.PackageId));

                if(hasMatchingProjectReference || hasMatchingPackageReference)
                    result.Add(flexReferencedProject);
            }

            return result.OrderBy(flexReferencedProject => flexReferencedProject.PackageId, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
