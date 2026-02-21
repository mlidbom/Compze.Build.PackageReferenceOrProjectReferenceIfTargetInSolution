using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

partial class ManagedProject
{
    static class CsprojUpdater
    {
        public static void UpdateIfNeeded(ManagedProject project)
        {
            var referencedFlexReferences = project.FindFlexReferences();

            if(referencedFlexReferences.Count == 0)
                return;

            var document = XDocument.Load(project.CsprojFile.FullName);
            var rootElement = document.Root!;

            RemoveExistingFlexReferences(rootElement);
            AppendFlexReferencePairs(rootElement, project.CsprojFile, referencedFlexReferences);

            document.SaveWithoutDeclaration(project.CsprojFile.FullName);
            Console.WriteLine($"  Updated: {project.CsprojFile.FullName} ({referencedFlexReferences.Count} flex reference(s))");
        }

        static void RemoveExistingFlexReferences(XElement rootElement)
        {
            var conditionalItemGroups = rootElement.Elements("ItemGroup")
                                                   .Where(itemGroup =>
                                                    {
                                                        var condition = itemGroup.Attribute("Condition")?.Value ?? "";
                                                        return FlexReferences.Any(package => condition.Contains(package.PropertyName));
                                                    })
                                                   .ToList();

            foreach(var itemGroup in conditionalItemGroups)
                itemGroup.RemoveWithPrecedingComment();

            foreach(var itemGroup in rootElement.Elements("ItemGroup").ToList())
            {
                var referencesToRemove = itemGroup.Elements()
                                                  .Where(element => IsFlexReference(element))
                                                  .ToList();

                foreach(var reference in referencesToRemove)
                    reference.Remove();

                if(!itemGroup.HasElements)
                    itemGroup.RemoveWithPrecedingComment();
            }
        }

        static bool IsFlexReference(XElement element)
        {
            if(element.Name.LocalName == "PackageReference")
            {
                var includeName = element.Attribute("Include")?.Value;
                return includeName != null &&
                       FlexReferences.Any(package =>
                                              package.PackageId.EqualsIgnoreCase(includeName));
            }

            if(element.Name.LocalName == "ProjectReference")
            {
                var includePath = element.Attribute("Include")?.Value;
                if(includePath == null) return false;
                var fileName = Path.GetFileName(includePath);
                return FlexReferences.Any(package =>
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

    }
}
