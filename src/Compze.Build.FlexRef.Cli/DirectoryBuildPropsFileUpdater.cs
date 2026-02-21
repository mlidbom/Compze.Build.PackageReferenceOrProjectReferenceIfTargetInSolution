using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

class DirectoryBuildPropsFileUpdater
{
    const string FileName = "Directory.Build.props";

    readonly string _filePath;
    readonly XDocument _document;
    readonly XElement _rootElement;

    DirectoryBuildPropsFileUpdater(DirectoryInfo rootDirectory)
    {
        _filePath = Path.Combine(rootDirectory.FullName, FileName);

        if(File.Exists(_filePath))
        {
            _document = XDocument.Load(_filePath);
            _rootElement = _document.Root
                ?? throw new InvalidOperationException($"Invalid {FileName}: missing root element.");
        } else
        {
            _rootElement = new XElement("Project");
            _document = new XDocument(_rootElement);
        }
    }

    public static void UpdateOrCreate(DirectoryInfo rootDirectory)
    {
        var updater = new DirectoryBuildPropsFileUpdater(rootDirectory);
        updater.Update();
    }

    void Update()
    {
        RemoveExistingFlexRefImport();
        RemoveExistingUsePackageReferenceProperties();

        AddFlexRefImport();
        AddUsePackageReferenceProperties();

        XmlFileHelper.SaveWithoutDeclaration(_document, _filePath);
        Console.WriteLine($"  Updated: {_filePath}");
    }

    void RemoveExistingFlexRefImport()
    {
        var flexRefImports = _rootElement.Elements("Import")
            .Where(element => element.Attribute("Project")?.Value?.Contains("FlexRef.props") == true)
            .ToList();

        foreach(var importElement in flexRefImports)
            importElement.RemoveWithPrecedingComment();
    }

    void RemoveExistingUsePackageReferenceProperties()
    {
        foreach(var propertyGroup in _rootElement.Elements("PropertyGroup").ToList())
        {
            var propertiesToRemove = propertyGroup.Elements()
                .Where(element => element.Name.LocalName.StartsWith("UsePackageReference_"))
                .ToList();

            foreach(var property in propertiesToRemove)
                property.RemoveWithPrecedingComment();

            if(!propertyGroup.HasElements)
                propertyGroup.RemoveWithPrecedingComment();
        }
    }

    void AddFlexRefImport()
    {
        var importPath = FlexRefPropsFileWriter.GetMsBuildImportProjectValue();
        _rootElement.Add(
            new XComment(" Import FlexRef infrastructure (reads solution content) "),
            new XElement("Import", new XAttribute("Project", importPath)));
    }

    void AddUsePackageReferenceProperties()
    {
        var flexReferences = ManagedProject.FlexReferences;
        if(flexReferences.Count == 0) return;

        var sortedPackages = flexReferences
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var propertyGroup = new XElement("PropertyGroup");

        foreach(var package in sortedPackages)
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

        _rootElement.Add(
            new XComment(" Per-dependency auto-detection managed by FlexRef "),
            propertyGroup);
    }
}
