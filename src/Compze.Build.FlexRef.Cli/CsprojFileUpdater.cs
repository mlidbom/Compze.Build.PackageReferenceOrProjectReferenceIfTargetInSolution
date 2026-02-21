using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

static class CsprojFileUpdater
{
    public static void UpdateIfNeeded(DiscoveredProject project, List<SwitchablePackageInfo> switchablePackages)
    {
        var referencedSwitchablePackages = DetermineReferencedSwitchablePackages(project, switchablePackages);

        if (referencedSwitchablePackages.Count == 0)
            return;

        var document = XDocument.Load(project.CsprojFullPath);
        var rootElement = document.Root!;

        RemoveExistingSwitchableReferences(rootElement, switchablePackages);
        AppendSwitchableReferencePairs(rootElement, project.CsprojFullPath, referencedSwitchablePackages);

        XmlFileHelper.SaveWithoutDeclaration(document, project.CsprojFullPath);
        Console.WriteLine($"  Updated: {project.CsprojFullPath} ({referencedSwitchablePackages.Count} switchable reference(s))");
    }

    static List<SwitchablePackageInfo> DetermineReferencedSwitchablePackages(
        DiscoveredProject project,
        List<SwitchablePackageInfo> switchablePackages)
    {
        var result = new List<SwitchablePackageInfo>();

        foreach (var package in switchablePackages)
        {
            if (project.CsprojFullPath.Equals(package.CsprojFullPath, StringComparison.OrdinalIgnoreCase))
                continue;

            var hasMatchingProjectReference = project.ProjectReferences
                .Any(reference => reference.ResolvedFileName.Equals(package.CsprojFileName, StringComparison.OrdinalIgnoreCase));

            var hasMatchingPackageReference = project.PackageReferences
                .Any(reference => reference.PackageName.Equals(package.PackageId, StringComparison.OrdinalIgnoreCase));

            if (hasMatchingProjectReference || hasMatchingPackageReference)
                result.Add(package);
        }

        return result.OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static void RemoveExistingSwitchableReferences(XElement rootElement, List<SwitchablePackageInfo> switchablePackages)
    {
        // First pass: remove entire ItemGroups conditioned on UsePackageReference_* for switchable packages
        var conditionalItemGroups = rootElement.Elements("ItemGroup")
            .Where(itemGroup =>
            {
                var condition = itemGroup.Attribute("Condition")?.Value ?? "";
                return switchablePackages.Any(package => condition.Contains(package.PropertyName));
            })
            .ToList();

        foreach (var itemGroup in conditionalItemGroups)
            RemoveElementAndPrecedingComment(itemGroup);

        // Second pass: remove individual switchable references from remaining (unconditional) ItemGroups
        foreach (var itemGroup in rootElement.Elements("ItemGroup").ToList())
        {
            var referencesToRemove = itemGroup.Elements()
                .Where(element => IsReferenceToSwitchablePackage(element, switchablePackages))
                .ToList();

            foreach (var reference in referencesToRemove)
                reference.Remove();

            if (!itemGroup.HasElements)
                RemoveElementAndPrecedingComment(itemGroup);
        }
    }

    static bool IsReferenceToSwitchablePackage(XElement element, List<SwitchablePackageInfo> switchablePackages)
    {
        if (element.Name.LocalName == "PackageReference")
        {
            var includeName = element.Attribute("Include")?.Value;
            return includeName != null &&
                switchablePackages.Any(package =>
                    package.PackageId.Equals(includeName, StringComparison.OrdinalIgnoreCase));
        }

        if (element.Name.LocalName == "ProjectReference")
        {
            var includePath = element.Attribute("Include")?.Value;
            if (includePath == null) return false;
            var fileName = Path.GetFileName(includePath);
            return switchablePackages.Any(package =>
                package.CsprojFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    static void AppendSwitchableReferencePairs(
        XElement rootElement,
        string consumingCsprojFullPath,
        List<SwitchablePackageInfo> referencedPackages)
    {
        foreach (var package in referencedPackages)
        {
            var relativeProjectPath = ComputeRelativePathWithBackslashes(
                consumingCsprojFullPath, package.CsprojFullPath);

            rootElement.Add(
                new XComment($" {package.PackageId} — switchable reference "),
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
        if (node.PreviousNode is XComment)
            node.PreviousNode.Remove();
        node.Remove();
    }
}
