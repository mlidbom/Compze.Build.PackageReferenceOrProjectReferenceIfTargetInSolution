using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

class FlexRefConfigurationFile
{
    public bool UseAutoDiscover { get; init; }
    public List<string> AutoDiscoverExclusions { get; init; } = [];
    public List<string> ExplicitPackageNames { get; init; } = [];

    const string ConfigFileName = "FlexRef.config.xml";

    public static string GetConfigFilePath(string rootDirectory) =>
        Path.Combine(rootDirectory, ConfigFileName);

    public static bool ExistsIn(string rootDirectory) =>
        File.Exists(GetConfigFilePath(rootDirectory));

    public static FlexRefConfigurationFile LoadFrom(string rootDirectory)
    {
        var configFilePath = GetConfigFilePath(rootDirectory);
        var document = XDocument.Load(configFilePath);
        var rootElement = document.Root
            ?? throw new InvalidOperationException($"Invalid config file: {configFilePath} has no root element.");

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

        return new FlexRefConfigurationFile
        {
            UseAutoDiscover = autoDiscoverElement != null,
            AutoDiscoverExclusions = autoDiscoverExclusions,
            ExplicitPackageNames = explicitPackageNames,
        };
    }

    public static void CreateDefaultConfigFile(string rootDirectory, List<string> discoveredPackageIds)
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
        XmlFileHelper.SaveWithoutDeclaration(document, GetConfigFilePath(rootDirectory));
    }
}
