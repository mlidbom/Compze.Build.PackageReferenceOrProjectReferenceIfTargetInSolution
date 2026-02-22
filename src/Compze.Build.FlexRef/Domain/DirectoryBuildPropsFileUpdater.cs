using System.Xml.Linq;
using Compze.Build.FlexRef.SystemCE;

namespace Compze.Build.FlexRef.Domain;

class DirectoryBuildPropsFileUpdater
{
    readonly FileInfo _file;
    readonly XDocument _document;
    readonly XElement _rootElement;
    readonly FlexRefWorkspace _workspace;

    DirectoryBuildPropsFileUpdater(FlexRefWorkspace workspace)
    {
        _workspace = workspace;
        _file = new FileInfo(Path.Combine(workspace.RootDirectory.FullName, DomainConstants.DirectoryBuildPropsFileName));

        if(_file.Exists)
        {
            _document = XDocument.Load(_file.FullName);
            _rootElement = _document.Root
                ?? throw new InvalidOperationException($"Invalid {DomainConstants.DirectoryBuildPropsFileName}: missing root element.");
        } else
        {
            _rootElement = new XElement("Project");
            _document = new XDocument(_rootElement);
        }
    }

    public static void UpdateOrCreate(FlexRefWorkspace workspace)
    {
        var updater = new DirectoryBuildPropsFileUpdater(workspace);
        updater.Update();
    }

    void Update()
    {
        RemoveExistingFlexRefImport();
        RemoveExistingUsePackageReferenceProperties();

        AddFlexRefImport();
        AddUsePackageReferenceProperties();

        _document.SaveWithoutDeclaration(_file.FullName);
        Console.WriteLine($"  Updated: {_file.FullName}");
    }

    void RemoveExistingFlexRefImport()
    {
        var flexRefImports = _rootElement.Elements("Import")
            .Where(element => element.Attribute("Project")?.Value.Contains(DomainConstants.PropsFileName) == true)
            .ToList();

        foreach(var importElement in flexRefImports)
            importElement.RemoveWithPrecedingComment();
    }

    void RemoveExistingUsePackageReferenceProperties()
    {
        foreach(var propertyGroup in _rootElement.Elements("PropertyGroup").ToList())
        {
            var propertiesToRemove = propertyGroup.Elements()
                .Where(element => element.Name.LocalName.StartsWith(DomainConstants.UsePackageReferencePropertyPrefix))
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
        if(_workspace.FlexReferencedProjects.Count == 0) return;

        var sortedPackages = _workspace.FlexReferencedProjects
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var propertyGroup = new XElement("PropertyGroup");

        foreach(var package in sortedPackages)
        {
            var conditionValue =
                $"'$({package.PropertyName})' != 'true'"
                + $" And '$(_FlexRef_SolutionProjects)' != ''"
                + $" And !$(_FlexRef_SolutionProjects.Contains('|{package.CsprojFile.Name}|'))";

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
