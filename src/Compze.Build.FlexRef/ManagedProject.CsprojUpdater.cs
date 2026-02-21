using System.Xml.Linq;

namespace Compze.Build.FlexRef;

partial class ManagedProject
{
    static class CsprojUpdater
    {
        public static void UpdateIfNeeded(ManagedProject project, FlexRefWorkspace workspace)
        {
            var referencedFlexReferences = project.FindFlexReferences(workspace);

            if(referencedFlexReferences.Count == 0)
                return;

            var document = XDocument.Load(project.CsprojFile.FullName);
            var rootElement = document.Root!;

            RemoveExistingFlexReferences(rootElement, workspace.FlexReferences);
            AppendFlexReferencePairs(rootElement, project.CsprojFile, referencedFlexReferences);

            document.SaveWithoutDeclaration(project.CsprojFile.FullName);
            Console.WriteLine($"  Updated: {project.CsprojFile.FullName} ({referencedFlexReferences.Count} flex reference(s))");
        }

        static void RemoveExistingFlexReferences(XElement rootElement, IReadOnlyList<FlexReference> flexReferences)
        {
            var conditionalItemGroups = rootElement.Elements("ItemGroup")
                                                   .Where(itemGroup =>
                                                    {
                                                        var condition = itemGroup.Attribute("Condition")?.Value ?? "";
                                                        return flexReferences.Any(package => condition.Contains(package.PropertyName));
                                                    })
                                                   .ToList();

            foreach(var itemGroup in conditionalItemGroups)
                itemGroup.RemoveWithPrecedingComment();

            foreach(var itemGroup in rootElement.Elements("ItemGroup").ToList())
            {
                var referencesToRemove = itemGroup.Elements()
                                                  .Where(element => IsFlexReference(element, flexReferences))
                                                  .ToList();

                foreach(var reference in referencesToRemove)
                    reference.Remove();

                if(!itemGroup.HasElements)
                    itemGroup.RemoveWithPrecedingComment();
            }
        }

        static bool IsFlexReference(XElement element, IReadOnlyList<FlexReference> flexReferences)
        {
            if(element.Name.LocalName == "PackageReference")
            {
                var includeName = element.Attribute("Include")?.Value;
                return includeName != null &&
                       flexReferences.Any(package =>
                                              package.PackageId.EqualsIgnoreCase(includeName));
            }

            if(element.Name.LocalName == "ProjectReference")
            {
                var includePath = element.Attribute("Include")?.Value;
                if(includePath == null) return false;
                var fileName = Path.GetFileName(includePath);
                return flexReferences.Any(package =>
                                              package.CsprojFile.Name.EqualsIgnoreCase(fileName));
            }

            return false;
        }

        static void AppendFlexReferencePairs(
            XElement rootElement,
            FileInfo consumingCsprojFile,
            List<FlexReference> referencedPackages)
        {
            foreach(var package in referencedPackages)
            {
                var relativeProjectPath = ComputeRelativePathWithBackslashes(
                    consumingCsprojFile,
                    package.CsprojFile);

                rootElement.Add(
                    new XComment($" {package.PackageId} — flex reference "),
                    new XElement("ItemGroup",
                                 new XAttribute("Condition", $"'$({package.PropertyName})' == 'true'"),
                                 new XElement("PackageReference",
                                              new XAttribute("Include", package.PackageId),
                                              new XAttribute("Version", "*-*"))),
                    new XElement("ItemGroup",
                                 new XAttribute("Condition", $"'$({package.PropertyName})' != 'true'"),
                                 new XElement("ProjectReference",
                                              new XAttribute("Include", relativeProjectPath))));
            }
        }

        static string ComputeRelativePathWithBackslashes(FileInfo fromCsproj, FileInfo toCsproj)
        {
            var fromDirectory = fromCsproj.DirectoryName!;
            var relativePath = Path.GetRelativePath(fromDirectory, toCsproj.FullName);
            return relativePath.Replace('/', '\\');
        }

    }
}
