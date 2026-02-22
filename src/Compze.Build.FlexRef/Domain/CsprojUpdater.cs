using System.Xml.Linq;
using Compze.Build.FlexRef.SystemCE;

namespace Compze.Build.FlexRef.Domain;

class CsprojUpdater
{
    readonly FlexRefWorkspace _workspace;

    internal CsprojUpdater(FlexRefWorkspace workspace) =>
        _workspace = workspace;

    public void UpdateAll()
    {
        foreach(var project in _workspace.AllProjects)
            UpdateIfNeeded(project);
    }

    void UpdateIfNeeded(ManagedProject project)
    {
        if(project.FlexReferencedProjects.Count == 0)
            return;

        var document = XDocument.Load(project.CsprojFile.FullName);
        var rootElement = document.Root!;

        RemoveExistingFlexReferences(rootElement);
        AppendFlexReferencePairs(rootElement, project.CsprojFile, project.FlexReferencedProjects);

        document.SaveWithoutDeclaration(project.CsprojFile.FullName);
        Console.WriteLine($"  Updated: {project.CsprojFile.FullName} ({project.FlexReferencedProjects.Count} flex reference(s))");
    }

    void RemoveExistingFlexReferences(XElement rootElement)
    {
        var conditionalItemGroups = rootElement.Elements("ItemGroup")
                                               .Where(itemGroup =>
                                                {
                                                    var condition = itemGroup.Attribute("Condition")?.Value ?? "";
                                                    return _workspace.FlexReferencedProjects.Any(flexReferencedProject => condition.Contains(flexReferencedProject.PropertyName));
                                                })
                                               .ToList();

        foreach(var itemGroup in conditionalItemGroups)
            itemGroup.RemoveWithPrecedingComment();

        foreach(var itemGroup in rootElement.Elements("ItemGroup").ToList())
        {
            var referencesToRemove = itemGroup.Elements()
                                              .Where(IsReferenceToFlexReferencedProject)
                                              .ToList();

            foreach(var reference in referencesToRemove)
                reference.Remove();

            if(!itemGroup.HasElements)
                itemGroup.RemoveWithPrecedingComment();
        }
    }

    bool IsReferenceToFlexReferencedProject(XElement element)
    {
        if(element.Name.LocalName == "PackageReference")
        {
            var includeName = element.Attribute("Include")?.Value;
            return includeName != null &&
                   _workspace.FlexReferencedProjects.Any(flexReferencedProject =>
                                                     flexReferencedProject.PackageId.EqualsIgnoreCase(includeName));
        }

        if(element.Name.LocalName == "ProjectReference")
        {
            var includePath = element.Attribute("Include")?.Value;
            if(includePath == null) return false;
            var fileName = Path.GetFileName(includePath);
            return _workspace.FlexReferencedProjects.Any(flexReferencedProject =>
                                                     flexReferencedProject.CsprojFile.Name.EqualsIgnoreCase(fileName));
        }

        return false;
    }

    static void AppendFlexReferencePairs(XElement rootElement, FileInfo consumingCsprojFile, List<FlexReferencedProject> flexReferencedProjects)
    {
        foreach(var flexReferencedProject in flexReferencedProjects)
        {
            var relativeProjectPath = consumingCsprojFile.ComputeRelativePathWithBackslashes(flexReferencedProject.CsprojFile);

            rootElement.Add(
                new XComment($" {flexReferencedProject.PackageId} — flex reference "),
                new XElement("ItemGroup",
                             new XAttribute("Condition", $"'$({flexReferencedProject.PropertyName})' == 'true'"),
                             new XElement("PackageReference",
                                          new XAttribute("Include", flexReferencedProject.PackageId),
                                          new XAttribute("Version", "*-*"))),
                new XElement("ItemGroup",
                             new XAttribute("Condition", $"'$({flexReferencedProject.PropertyName})' != 'true'"),
                             new XElement("ProjectReference",
                                          new XAttribute("Include", relativeProjectPath))));
        }
    }
}
