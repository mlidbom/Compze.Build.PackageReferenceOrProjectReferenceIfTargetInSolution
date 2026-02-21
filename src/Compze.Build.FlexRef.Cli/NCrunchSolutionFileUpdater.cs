using System.Xml.Linq;

namespace Compze.Build.FlexRef.Cli;

static class NCrunchSolutionFileUpdater
{
    public static void UpdateOrCreate(SlnxSolutionInfo solution, List<SwitchablePackageInfo> switchablePackages)
    {
        var absentPackages = switchablePackages
            .Where(package => !solution.ProjectFileNames
                .Contains(package.CsprojFileName, StringComparer.OrdinalIgnoreCase))
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ncrunchFilePath = DeriveNCrunchFilePath(solution.SlnxFullPath);

        if (File.Exists(ncrunchFilePath))
            UpdateExistingNCrunchFile(ncrunchFilePath, absentPackages);
        else
            CreateNewNCrunchFile(ncrunchFilePath, absentPackages);
    }

    static string DeriveNCrunchFilePath(string slnxFullPath)
    {
        var directory = Path.GetDirectoryName(slnxFullPath)!;
        var solutionStem = Path.GetFileNameWithoutExtension(slnxFullPath);
        return Path.Combine(directory, solutionStem + ".v3.ncrunchsolution");
    }

    static void CreateNewNCrunchFile(string filePath, List<SwitchablePackageInfo> absentPackages)
    {
        var settingsElement = new XElement("Settings");

        if (absentPackages.Count > 0)
        {
            var customBuildProperties = new XElement("CustomBuildProperties");
            foreach (var package in absentPackages)
                customBuildProperties.Add(new XElement("Value", $"{package.PropertyName} = true"));
            settingsElement.Add(customBuildProperties);
        }

        var document = new XDocument(
            new XElement("SolutionConfiguration", settingsElement));

        XmlFileHelper.SaveWithoutDeclaration(document, filePath);
        Console.WriteLine($"  Created: {filePath} ({absentPackages.Count} absent package(s))");
    }

    static void UpdateExistingNCrunchFile(string filePath, List<SwitchablePackageInfo> absentPackages)
    {
        var document = XDocument.Load(filePath);
        var rootElement = document.Root!;

        var settingsElement = rootElement.Element("Settings");
        if (settingsElement == null)
        {
            settingsElement = new XElement("Settings");
            rootElement.Add(settingsElement);
        }

        var customBuildProperties = settingsElement.Element("CustomBuildProperties");

        // Remove existing FlexRef entries (UsePackageReference_*) but keep other custom build properties
        if (customBuildProperties != null)
        {
            var existingFlexRefValues = customBuildProperties.Elements("Value")
                .Where(value => value.Value.TrimStart().StartsWith("UsePackageReference_"))
                .ToList();

            foreach (var value in existingFlexRefValues)
                value.Remove();
        }

        // Append new FlexRef entries at the end
        if (absentPackages.Count > 0)
        {
            if (customBuildProperties == null)
            {
                customBuildProperties = new XElement("CustomBuildProperties");
                settingsElement.Add(customBuildProperties);
            }

            foreach (var package in absentPackages)
                customBuildProperties.Add(new XElement("Value", $"{package.PropertyName} = true"));
        }

        // Remove CustomBuildProperties element entirely if it has no entries
        if (customBuildProperties != null && !customBuildProperties.HasElements)
            customBuildProperties.Remove();

        XmlFileHelper.SaveWithoutDeclaration(document, filePath);
        Console.WriteLine($"  Updated: {filePath} ({absentPackages.Count} absent package(s))");
    }
}
