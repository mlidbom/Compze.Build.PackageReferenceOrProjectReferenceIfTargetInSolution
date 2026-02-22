using System.Xml.Linq;
using Compze.Build.FlexRef.SystemCE;
using Compze.Build.FlexRef.SystemCE.IOCE;
using Compze.Build.FlexRef.SystemCE.XmlCE.LinqCE;

namespace Compze.Build.FlexRef.Domain;

partial class CSProj
{
    internal static void UpdateAll(FlexRefWorkspace workspace)
    {
        foreach(var project in workspace.AllProjects)
            new Updater(workspace).UpdateIfNeeded(project);
    }

    class Updater
    {
        readonly FlexRefWorkspace _workspace;

        internal Updater(FlexRefWorkspace workspace) =>
            _workspace = workspace;

        internal void UpdateIfNeeded(CSProj project)
        {
            if(project.FlexReferencedProjects.Count == 0)
                return;

            var document = XDocument.Load(project.CsprojFile.FullName);
            var rootElement = document.Root!;

            var existingPackageVersions = ExtractExistingPackageVersions(rootElement);
            RemoveExistingFlexReferences(rootElement);
            AppendFlexReferencePairs(rootElement, project, existingPackageVersions);

            document.SaveWithoutDeclaration(project.CsprojFile.FullName);
            Console.WriteLine($"  Updated: {project.CsprojFile.FullName} ({project.FlexReferencedProjects.Count} flex reference(s))");
        }

        Dictionary<string, string> ExtractExistingPackageVersions(XElement rootElement)
        {
            var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach(var packageRef in rootElement.Elements("ItemGroup").Elements("PackageReference"))
            {
                var includeName = packageRef.Attribute("Include")?.Value;
                var version = packageRef.Attribute("Version")?.Value;

                if(includeName != null && version != null &&
                   _workspace.FlexReferencedProjects.Any(flexReferencedProject => flexReferencedProject.PackageId.EqualsIgnoreCase(includeName)))
                    versions[includeName] = version;
            }

            return versions;
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
                var fileName = Path.GetFileName(includePath.Replace('\\', '/'));
                return _workspace.FlexReferencedProjects.Any(flexReferencedProject =>
                                                         flexReferencedProject.CsprojFile.Name.EqualsIgnoreCase(fileName));
            }

            return false;
        }

        static void AppendFlexReferencePairs(XElement rootElement, CSProj project, Dictionary<string, string> existingPackageVersions)
        {
            foreach(var flexReferencedProject in project.FlexReferencedProjects)
            {
                var relativeProjectPath = project.CsprojFile.ComputeRelativePathWithBackslashes(flexReferencedProject.CsprojFile);

                var version = existingPackageVersions.TryGetValue(flexReferencedProject.PackageId, out var existingVersion)
                                  ? existingVersion
                                  : "*-*";

                rootElement.Add(
                    new XComment($" {flexReferencedProject.PackageId} — flex reference "),
                    new XElement("ItemGroup",
                                 new XAttribute("Condition", $"'$({flexReferencedProject.PropertyName})' == 'true'"),
                                 new XElement("PackageReference",
                                              new XAttribute("Include", flexReferencedProject.PackageId),
                                              new XAttribute("Version", version))),
                    new XElement("ItemGroup",
                                 new XAttribute("Condition", $"'$({flexReferencedProject.PropertyName})' != 'true'"),
                                 new XElement("ProjectReference",
                                              new XAttribute("Include", relativeProjectPath))));
            }
        }
    }
}
