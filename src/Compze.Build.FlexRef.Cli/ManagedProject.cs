using System.Xml.Linq;
using Microsoft.Build.Evaluation;

namespace Compze.Build.FlexRef.Cli;

record ProjectReferenceEntry(string IncludePath, string ResolvedFileName);

record PackageReferenceEntry(string PackageName, string Version);

partial class ManagedProject
{
    public FileInfo CsprojFile { get; }
    public string? PackageId { get; }
    public bool IsPackable { get; }
    public List<ProjectReferenceEntry> ProjectReferences { get; }
    public List<PackageReferenceEntry> PackageReferences { get; }

    public ManagedProject(FileInfo csprojFile, ProjectCollection projectCollection)
    {
        CsprojFile = csprojFile;

        var msbuildProject = new Project(csprojFile.FullName, null, null, projectCollection);

        var explicitPackageId = msbuildProject.GetNonEmptyPropertyOrNull("PackageId");
        var isPackableValue = msbuildProject.GetNonEmptyPropertyOrNull("IsPackable");
        var isExplicitlyNotPackable = string.Equals(isPackableValue, "false", StringComparison.OrdinalIgnoreCase);
        var isExplicitlyPackable = string.Equals(isPackableValue, "true", StringComparison.OrdinalIgnoreCase);
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

    public void UpdateCsprojIfNeeded(List<FlexReference> flexReferences) =>
        CsprojUpdater.UpdateIfNeeded(this, flexReferences);

    public List<FlexReference> FindFlexReferences(List<FlexReference> flexReferences)
    {
        var result = new List<FlexReference>();

        foreach(var package in flexReferences)
        {
            if(CsprojFile.FullName.Equals(package.CsprojFile.FullName, StringComparison.OrdinalIgnoreCase))
                continue;

            var hasMatchingProjectReference = ProjectReferences
                                                     .Any(reference => reference.ResolvedFileName.Equals(package.CsprojFile.Name, StringComparison.OrdinalIgnoreCase));

            var hasMatchingPackageReference = PackageReferences
                                                     .Any(reference => reference.PackageName.Equals(package.PackageId, StringComparison.OrdinalIgnoreCase));

            if(hasMatchingProjectReference || hasMatchingPackageReference)
                result.Add(package);
        }

        return result.OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
