namespace Compze.Build.FlexRef;

class FlexRefWorkspace
{
    public DirectoryInfo RootDirectory { get; }
    public IReadOnlyList<ManagedProject> AllProjects { get; }
    public IReadOnlyList<FlexReference> FlexReferences { get; }

    internal FlexRefWorkspace(DirectoryInfo rootDirectory, List<ManagedProject> allProjects, List<FlexReference> flexReferences)
    {
        RootDirectory = rootDirectory;
        AllProjects = allProjects;
        FlexReferences = flexReferences;
    }

    public static FlexRefWorkspace ScanAndResolve(DirectoryInfo rootDirectory, FlexRefConfigurationFile configuration)
    {
        var allProjects = ManagedProject.ScanDirectory(rootDirectory);
        var flexReferences = ManagedProject.ResolveFlexReferences(configuration, allProjects);
        return new FlexRefWorkspace(rootDirectory, allProjects, flexReferences);
    }

    public List<FlexReference> FindFlexReferencesFor(ManagedProject project)
    {
        var result = new List<FlexReference>();

        foreach(var flexReference in FlexReferences)
        {
            if(project.CsprojFile.FullName.EqualsIgnoreCase(flexReference.CsprojFile.FullName))
                continue;

            var hasMatchingProjectReference = project.ProjectReferences
                                                     .Any(reference => reference.ResolvedFileName.EqualsIgnoreCase(flexReference.CsprojFile.Name));

            var hasMatchingPackageReference = project.PackageReferences
                                                     .Any(reference => reference.PackageName.EqualsIgnoreCase(flexReference.PackageId));

            if(hasMatchingProjectReference || hasMatchingPackageReference)
                result.Add(flexReference);
        }

        return result.OrderBy(flexReference => flexReference.PackageId, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public List<FlexReference> FindAbsentFlexReferencesFor(SlnxSolution solution) =>
        FlexReferences
            .Where(package => !solution.ProjectFileNames
                                       .Contains(package.CsprojFile.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public void UpdateDirectoryBuildProps() =>
        DirectoryBuildPropsFileUpdater.UpdateOrCreate(this);

    public void UpdateCsprojFiles()
    {
        var updater = new CsprojUpdater(this);
        foreach(var project in AllProjects)
            updater.UpdateIfNeeded(project);
    }

    public void UpdateNCrunchFiles()
    {
        var updater = new NCrunchUpdater(this);
        var solutions = SlnxSolution.FindAndParseAllSolutions(RootDirectory);
        foreach(var solution in solutions)
            updater.UpdateOrCreate(solution);
    }
}
