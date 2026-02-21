using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

class FlexRefConfigurationFile
{
    const string ConfigFileName = "FlexRef.config.xml";

    static class Tags
    {
        public const string FlexRef = "FlexRef";
        public const string AutoDiscover = "AutoDiscover";
        public const string Exclude = "Exclude";
        public const string Package = "Package";
    }

    static class Attributes
    {
        public const string Name = "Name";
    }

    public DirectoryInfo RootDirectory { get; }
    public string ConfigFilePath { get; }
    public bool UseAutoDiscover { get; private set; }
    public List<string> AutoDiscoverExclusions { get; private set; } = [];
    public List<string> ExplicitPackageNames { get; private set; } = [];

    public FlexRefConfigurationFile(DirectoryInfo rootDirectory)
    {
        RootDirectory = rootDirectory;
        ConfigFilePath = Path.Combine(rootDirectory.FullName, ConfigFileName);
    }

    public bool Exists() => File.Exists(ConfigFilePath);

    public void Load()
    {
        var document = XDocument.Load(ConfigFilePath);
        var rootElement = document.Root
                       ?? throw new InvalidOperationException($"Invalid config file: {ConfigFilePath} has no root element.");

        var autoDiscoverElement = rootElement.Element(Tags.AutoDiscover);

        UseAutoDiscover = autoDiscoverElement != null;

        AutoDiscoverExclusions = autoDiscoverElement?
                                .Elements(Tags.Exclude)
                                .Select(element => element.Attribute(Attributes.Name)?.Value)
                                .Where(name => name != null)
                                .Select(name => name!)
                                .ToList() ?? [];

        ExplicitPackageNames = rootElement
                              .Elements(Tags.Package)
                              .Select(element => element.Attribute(Attributes.Name)?.Value)
                              .Where(name => name != null)
                              .Select(name => name!)
                              .ToList();
    }

    public void CreateDefault()
    {
        Console.WriteLine("Scanning for packable projects...");
        var allProjects = ManagedProject.ScanAllProjects(RootDirectory);
        var packableProjects = allProjects
            .Where(project => project is { IsPackable: true, PackageId: not null })
            .OrderBy(project => project.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine($"  Found {packableProjects.Count} packable project(s):");
        foreach (var project in packableProjects)
        {
            Console.WriteLine($"    - {project.PackageId} ({project.CsprojFile.Name})");

            var expectedFileName = project.PackageId + ".csproj";
            if (!project.CsprojFile.Name.EqualsIgnoreCase(expectedFileName))
                Console.Error.WriteLine($"      Warning: Package ID '{project.PackageId}' does not match file name '{project.CsprojFile.Name}'");
        }

        var packageIds = packableProjects.Select(project => project.PackageId!).ToList();
        WriteDefaultConfigFile(packageIds);
        Console.WriteLine($"  Created: {ConfigFilePath}");
    }

    void WriteDefaultConfigFile(List<string> discoveredPackageIds)
    {
        var sortedPackageIds = discoveredPackageIds
                              .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                              .ToList();

        var rootElement = new XElement(
            Tags.FlexRef,
            new XElement(Tags.AutoDiscover));

        if(sortedPackageIds.Count > 0)
        {
            //sadly the XComments do not work like real Xml nodes and can only be built using a string, so we have to build the comment text manually here
            var packageLines = string.Join("\n", sortedPackageIds.Select(id => $"  <{Tags.Package} {Attributes.Name}=\"{id}\" />"));
            rootElement.Add(new XComment(
                                $" Alternatively, list packages explicitly instead of using {Tags.AutoDiscover}:\n{packageLines}\n  "));
        }

        var document = new XDocument(rootElement);
        document.SaveWithoutDeclaration(ConfigFilePath);
    }
}
