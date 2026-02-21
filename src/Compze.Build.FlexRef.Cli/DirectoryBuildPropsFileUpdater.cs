using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

static class DirectoryBuildPropsFileUpdater
{
    const string FileName = "Directory.Build.props";

    public static void UpdateOrCreate(DirectoryInfo rootDirectory, List<FlexReference> flexReferences)
    {
        var filePath = Path.Combine(rootDirectory.FullName, FileName);
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
        AddUsePackageReferenceProperties(rootElement, flexReferences);

        XmlFileHelper.SaveWithoutDeclaration(document, filePath);
        Console.WriteLine($"  Updated: {filePath}");
    }

    static void RemoveExistingFlexRefImport(XElement rootElement)
    {
        var flexRefImports = rootElement.Elements("Import")
            .Where(element => element.Attribute("Project")?.Value?.Contains("FlexRef.props") == true)
            .ToList();

        foreach (var importElement in flexRefImports)
            importElement.RemoveWithPrecedingComment();
    }

    static void RemoveExistingUsePackageReferenceProperties(XElement rootElement)
    {
        foreach (var propertyGroup in rootElement.Elements("PropertyGroup").ToList())
        {
            var propertiesToRemove = propertyGroup.Elements()
                .Where(element => element.Name.LocalName.StartsWith("UsePackageReference_"))
                .ToList();

            foreach (var property in propertiesToRemove)
                property.RemoveWithPrecedingComment();

            if (!propertyGroup.HasElements)
                propertyGroup.RemoveWithPrecedingComment();
        }
    }

    static void AddFlexRefImport(XElement rootElement)
    {
        var importPath = FlexRefPropsFileWriter.GetMsBuildImportProjectValue();
        rootElement.Add(
            new XComment(" Import FlexRef infrastructure (reads solution content) "),
            new XElement("Import", new XAttribute("Project", importPath)));
    }

    static void AddUsePackageReferenceProperties(XElement rootElement, List<FlexReference> flexReferences)
    {
        if (flexReferences.Count == 0) return;

        var sortedPackages = flexReferences
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var propertyGroup = new XElement("PropertyGroup");

        foreach (var package in sortedPackages)
        {
            var conditionValue =
                $"'$({package.PropertyName})' != 'true'"
                + $" And '$(_SwitchRef_SolutionProjects)' != ''"
                + $" And !$(_SwitchRef_SolutionProjects.Contains('|{package.CsprojFile.Name}|'))";

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

}
