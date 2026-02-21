using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

partial class ManagedProject
{
    static class CsprojUpdater
    {
        public static void UpdateIfNeeded(ManagedProject project, List<FlexReference> flexReferences)
        {
            var referencedFlexReferences = DetermineReferencedFlexReferences(project, flexReferences);

            if(referencedFlexReferences.Count == 0)
                return;

            var document = XDocument.Load(project.CsprojFile.FullName);
            var rootElement = document.Root!;

            RemoveExistingFlexReferences(rootElement, flexReferences);
            AppendFlexReferencePairs(rootElement, project.CsprojFile, referencedFlexReferences);

            XmlFileHelper.SaveWithoutDeclaration(document, project.CsprojFile.FullName);
            Console.WriteLine($"  Updated: {project.CsprojFile.FullName} ({referencedFlexReferences.Count} flex reference(s))");
        }

        static List<FlexReference> DetermineReferencedFlexReferences(ManagedProject project, List<FlexReference> flexReferences)
        {
            var result = new List<FlexReference>();

            foreach(var package in flexReferences)
            {
                if(project.CsprojFile.FullName.Equals(package.CsprojFile.FullName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var hasMatchingProjectReference = project.ProjectReferences
                                                         .Any(reference => reference.ResolvedFileName.Equals(package.CsprojFile.Name, StringComparison.OrdinalIgnoreCase));

                var hasMatchingPackageReference = project.PackageReferences
                                                         .Any(reference => reference.PackageName.Equals(package.PackageId, StringComparison.OrdinalIgnoreCase));

                if(hasMatchingProjectReference || hasMatchingPackageReference)
                    result.Add(package);
            }

            return result.OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase).ToList();
        }

        static void RemoveExistingFlexReferences(XElement rootElement, List<FlexReference> flexReferences)
        {
            var conditionalItemGroups = rootElement.Elements("ItemGroup")
                                                   .Where(itemGroup =>
                                                    {
                                                        var condition = itemGroup.Attribute("Condition")?.Value ?? "";
                                                        return flexReferences.Any(package => condition.Contains(package.PropertyName));
                                                    })
                                                   .ToList();

            foreach(var itemGroup in conditionalItemGroups)
                RemoveElementAndPrecedingComment(itemGroup);

            foreach(var itemGroup in rootElement.Elements("ItemGroup").ToList())
            {
                var referencesToRemove = itemGroup.Elements()
                                                  .Where(element => IsFlexReference(element, flexReferences))
                                                  .ToList();

                foreach(var reference in referencesToRemove)
                    reference.Remove();

                if(!itemGroup.HasElements)
                    RemoveElementAndPrecedingComment(itemGroup);
            }
        }

        static bool IsFlexReference(XElement element, List<FlexReference> flexReferences)
        {
            if(element.Name.LocalName == "PackageReference")
            {
                var includeName = element.Attribute("Include")?.Value;
                return includeName != null &&
                       flexReferences.Any(package =>
                                              package.PackageId.Equals(includeName, StringComparison.OrdinalIgnoreCase));
            }

            if(element.Name.LocalName == "ProjectReference")
            {
                var includePath = element.Attribute("Include")?.Value;
                if(includePath == null) return false;
                var fileName = Path.GetFileName(includePath);
                return flexReferences.Any(package =>
                                              package.CsprojFile.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
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
                    consumingCsprojFile.FullName,
                    package.CsprojFile.FullName);

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

        static string ComputeRelativePathWithBackslashes(string fromCsprojFullPath, string toCsprojFullPath)
        {
            var fromDirectory = Path.GetDirectoryName(fromCsprojFullPath)!;
            var relativePath = Path.GetRelativePath(fromDirectory, toCsprojFullPath);
            return relativePath.Replace('/', '\\');
        }

        static void RemoveElementAndPrecedingComment(XNode node)
        {
            if(node.PreviousNode is XComment)
                node.PreviousNode.Remove();
            node.Remove();
        }
    }
}
