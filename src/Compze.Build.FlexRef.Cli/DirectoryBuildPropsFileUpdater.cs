using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

static class DirectoryBuildPropsFileUpdater
{
    const string FileName = "Directory.Build.props";

    public static void UpdateOrCreate(string rootDirectory, List<SwitchablePackageInfo> switchablePackages)
    {
        var filePath = Path.Combine(rootDirectory, FileName);
        XDocument document;
        XElement rootElement;

        if (File.Exists(filePath))
        {
            document = XDocument.Load(filePath);
            rootElement = document.Root
                ?? throw new InvalidOperationException($"Invalid {FileName}: missing root element.");
        }
        else
        {
            rootElement = new XElement("Project");
            document = new XDocument(rootElement);
        }

        RemoveExistingFlexRefImport(rootElement);
        RemoveExistingUsePackageReferenceProperties(rootElement);

        AddFlexRefImport(rootElement);
        AddUsePackageReferenceProperties(rootElement, switchablePackages);

        XmlFileHelper.SaveWithoutDeclaration(document, filePath);
        Console.WriteLine($"  Updated: {filePath}");
    }

    static void RemoveExistingFlexRefImport(XElement rootElement)
    {
        var flexRefImports = rootElement.Elements("Import")
            .Where(element => element.Attribute("Project")?.Value?.Contains("FlexRef.props") == true)
            .ToList();

        foreach (var importElement in flexRefImports)
            RemoveElementAndPrecedingComment(importElement);
    }

    static void RemoveExistingUsePackageReferenceProperties(XElement rootElement)
    {
        foreach (var propertyGroup in rootElement.Elements("PropertyGroup").ToList())
        {
            var propertiesToRemove = propertyGroup.Elements()
                .Where(element => element.Name.LocalName.StartsWith("UsePackageReference_"))
                .ToList();

            foreach (var property in propertiesToRemove)
                RemoveElementAndPrecedingComment(property);

            if (!propertyGroup.HasElements)
                RemoveElementAndPrecedingComment(propertyGroup);
        }
    }

    static void AddFlexRefImport(XElement rootElement)
    {
        var importPath = FlexRefPropsFileWriter.GetMsBuildImportProjectValue();
        rootElement.Add(
            new XComment(" Import FlexRef infrastructure (reads solution content) "),
            new XElement("Import", new XAttribute("Project", importPath)));
    }

    static void AddUsePackageReferenceProperties(XElement rootElement, List<SwitchablePackageInfo> switchablePackages)
    {
        if (switchablePackages.Count == 0) return;

        var sortedPackages = switchablePackages
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var propertyGroup = new XElement("PropertyGroup");

        foreach (var package in sortedPackages)
        {
            var conditionValue =
                $"'$({package.PropertyName})' != 'true'"
                + $" And '$(_SwitchRef_SolutionProjects)' != ''"
                + $" And !$(_SwitchRef_SolutionProjects.Contains('|{package.CsprojFileName}|'))";

            propertyGroup.Add(
                new XComment($" {package.PackageId} "),
                new XElement(package.PropertyName,
                    new XAttribute("Condition", conditionValue),
                    "true"));
        }

        rootElement.Add(
            new XComment(" Per-dependency auto-detection managed by FlexRef "),
            propertyGroup);
    }

    static void RemoveElementAndPrecedingComment(XNode node)
    {
        if (node.PreviousNode is XComment)
            node.PreviousNode.Remove();
        node.Remove();
    }
}
