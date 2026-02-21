using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

class FlexRefConfigurationFile
{
    const string ConfigFileName = "FlexRef.config.xml";

    public DirectoryInfo RootDirectory { get; }
    public string ConfigFilePath { get; }
    public bool UseAutoDiscover { get; private init; }
    public List<string> AutoDiscoverExclusions { get; private init; } = [];
    public List<string> ExplicitPackageNames { get; private init; } = [];

    public FlexRefConfigurationFile(DirectoryInfo rootDirectory)
    {
        RootDirectory = rootDirectory;
        ConfigFilePath = Path.Combine(rootDirectory.FullName, ConfigFileName);
    }

    public bool Exists() => File.Exists(ConfigFilePath);

    public FlexRefConfigurationFile Load()
    {
        var document = XDocument.Load(ConfigFilePath);
        var rootElement = document.Root
            ?? throw new InvalidOperationException($"Invalid config file: {ConfigFilePath} has no root element.");

        var autoDiscoverElement = rootElement.Element("AutoDiscover");

        var autoDiscoverExclusions = autoDiscoverElement?
            .Elements("Exclude")
            .Select(element => element.Attribute("Name")?.Value)
            .Where(name => name != null)
            .Select(name => name!)
            .ToList() ?? [];

        var explicitPackageNames = rootElement
            .Elements("Package")
            .Select(element => element.Attribute("Name")?.Value)
            .Where(name => name != null)
            .Select(name => name!)
            .ToList();

        return new FlexRefConfigurationFile(RootDirectory)
        {
            UseAutoDiscover = autoDiscoverElement != null,
            AutoDiscoverExclusions = autoDiscoverExclusions,
            ExplicitPackageNames = explicitPackageNames,
        };
    }

    public void CreateDefaultConfigFile(List<string> discoveredPackageIds)
    {
        var sortedPackageIds = discoveredPackageIds
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rootElement = new XElement("FlexRef",
            new XElement("AutoDiscover"));

        if (sortedPackageIds.Count > 0)
        {
            var packageLines = string.Join("\n", sortedPackageIds.Select(id => $"  <Package Name=\"{id}\" />"));
            rootElement.Add(new XComment(
                $" Alternatively, list packages explicitly instead of using AutoDiscover:\n{packageLines}\n  "));
        }

        var document = new XDocument(rootElement);
        XmlFileHelper.SaveWithoutDeclaration(document, ConfigFilePath);
    }
}
