using System.Xml.Linq;

namespace Compze.Build.FlexRef;

class NCrunchUpdater
{
    readonly FlexRefWorkspace _workspace;

    internal NCrunchUpdater(FlexRefWorkspace workspace) =>
        _workspace = workspace;

    public void UpdateOrCreate(SlnxSolution solution)
    {
        var absentPackages = solution.FindAbsentFlexReferences(_workspace.FlexReferences);
        var ncrunchFile = solution.NCrunchFile;

        if(ncrunchFile.Exists)
            UpdateExistingNCrunchFile(ncrunchFile, absentPackages);
        else
            CreateNewNCrunchFile(ncrunchFile, absentPackages);
    }

    static void CreateNewNCrunchFile(FileInfo file, List<FlexReference> absentPackages)
    {
        var settingsElement = new XElement("Settings");

        if(absentPackages.Count > 0)
        {
            var customBuildProperties = new XElement("CustomBuildProperties");
            foreach(var package in absentPackages)
                customBuildProperties.Add(new XElement("Value", $"{package.PropertyName} = true"));
            settingsElement.Add(customBuildProperties);
        }

        var document = new XDocument(
            new XElement("SolutionConfiguration", settingsElement));

        document.SaveWithoutDeclaration(file.FullName);
        Console.WriteLine($"  Created: {file.FullName} ({absentPackages.Count} absent package(s))");
    }

    static void UpdateExistingNCrunchFile(FileInfo file, List<FlexReference> absentPackages)
    {
        var document = XDocument.Load(file.FullName);
        var rootElement = document.Root!;

        var settingsElement = rootElement.Element("Settings");
        if(settingsElement == null)
        {
            settingsElement = new XElement("Settings");
            rootElement.Add(settingsElement);
        }

        var customBuildProperties = settingsElement.Element("CustomBuildProperties");

        // Remove existing FlexRef entries (UsePackageReference_*) but keep other custom build properties
        if(customBuildProperties != null)
        {
            var existingFlexRefValues = customBuildProperties.Elements("Value")
                .Where(value => value.Value.TrimStart().StartsWith("UsePackageReference_"))
                .ToList();

            foreach(var value in existingFlexRefValues)
                value.Remove();
        }

        // Append new FlexRef entries at the end
        if(absentPackages.Count > 0)
        {
            if(customBuildProperties == null)
            {
                customBuildProperties = new XElement("CustomBuildProperties");
                settingsElement.Add(customBuildProperties);
            }

            foreach(var package in absentPackages)
                customBuildProperties.Add(new XElement("Value", $"{package.PropertyName} = true"));
        }

        // Remove CustomBuildProperties element entirely if it has no entries
        if(customBuildProperties is { HasElements: false })
            customBuildProperties.Remove();

        document.SaveWithoutDeclaration(file.FullName);
        Console.WriteLine($"  Updated: {file.FullName} ({absentPackages.Count} absent package(s))");
    }
}
