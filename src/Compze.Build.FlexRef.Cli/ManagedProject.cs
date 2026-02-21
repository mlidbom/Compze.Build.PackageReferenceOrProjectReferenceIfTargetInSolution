using Microsoft.Build.Evaluation;

namespace Compze.Build.FlexRef.Cli;

record ProjectReferenceEntry(string IncludePath, string ResolvedFileName);

record PackageReferenceEntry(string PackageName, string Version);

partial class ManagedProject
{
    static List<FlexReference>? _flexReferences;
    static List<ManagedProject>? _allProjects;

    public static IReadOnlyList<FlexReference> FlexReferences =>
        _flexReferences ?? throw new InvalidOperationException($"Call {nameof(ScanAndResolveFlexReferences)} first.");

    public static IReadOnlyList<ManagedProject> AllProjects =>
        _allProjects ?? throw new InvalidOperationException($"Call {nameof(ScanAllProjects)} or {nameof(ScanAndResolveFlexReferences)} first.");

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
        IsPackable = !isPackableValue.EqualsIgnoreCase("false") && (explicitPackageId != null || isPackableValue.EqualsIgnoreCase("true"));

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

    public static List<ManagedProject> ScanAllProjects(DirectoryInfo rootDirectory)
    {
        if(_allProjects != null)
            throw new InvalidOperationException("Projects have already been scanned. Scanning may only be performed once.");

        _allProjects = Scanner.ScanAllProjects(rootDirectory);
        return _allProjects;
    }

    public static List<FlexReference> ScanAndResolveFlexReferences(DirectoryInfo rootDirectory, FlexRefConfigurationFile configuration)
    {
        var allProjects = ScanAllProjects(rootDirectory);
        _flexReferences = FlexReferenceResolver.Resolve(configuration, allProjects);
        return _flexReferences;
    }

    public void UpdateCsprojIfNeeded() =>
        CsprojUpdater.UpdateIfNeeded(this);

    public List<FlexReference> FindFlexReferences()
    {
        var result = new List<FlexReference>();

        foreach(var package in FlexReferences)
        {
            if(CsprojFile.FullName.EqualsIgnoreCase(package.CsprojFile.FullName))
                continue;

            var hasMatchingProjectReference = ProjectReferences
                                                     .Any(reference => reference.ResolvedFileName.EqualsIgnoreCase(package.CsprojFile.Name));

            var hasMatchingPackageReference = PackageReferences
                                                     .Any(reference => reference.PackageName.EqualsIgnoreCase(package.PackageId));

            if(hasMatchingProjectReference || hasMatchingPackageReference)
                result.Add(package);
        }

        return result.OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
