using System.Xml.Linq;

namespace Compze.Build.FlexRef.Domain;

class NCrunchUpdater
{
    readonly FlexRefWorkspace _workspace;

    internal NCrunchUpdater(FlexRefWorkspace workspace) =>
        _workspace = workspace;

    public void UpdateAll()
    {
        var solutions = SlnxSolution.FindAndParseAllSolutions(_workspace.RootDirectory);
        foreach(var solution in solutions)
        {
            solution.Workspace = _workspace;
            UpdateOrCreate(solution);
        }
    }

    void UpdateOrCreate(SlnxSolution solution)
    {
        var ncrunchFile = solution.NCrunchFile;

        if(ncrunchFile.Exists)
            UpdateExistingNCrunchFile(ncrunchFile, solution.AbsentFlexReferencedProjects);
        else
            CreateNewNCrunchFile(ncrunchFile, solution.AbsentFlexReferencedProjects);
    }

    static void CreateNewNCrunchFile(FileInfo file, List<FlexReferencedProject> absentFlexReferencedProjects)
    {
        var settingsElement = new XElement("Settings");

        if(absentFlexReferencedProjects.Count > 0)
        {
            var customBuildProperties = new XElement("CustomBuildProperties");
            foreach(var flexReferencedProject in absentFlexReferencedProjects)
                customBuildProperties.Add(new XElement("Value", $"{flexReferencedProject.PropertyName} = true"));
            settingsElement.Add(customBuildProperties);
        }

        var document = new XDocument(
            new XElement("SolutionConfiguration", settingsElement));

        document.SaveWithoutDeclaration(file.FullName);
        Console.WriteLine($"  Created: {file.FullName} ({absentFlexReferencedProjects.Count} absent package(s))");
    }

    static void UpdateExistingNCrunchFile(FileInfo file, List<FlexReferencedProject> absentFlexReferencedProjects)
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
                .Where(value => value.Value.TrimStart().StartsWith(DomainConstants.UsePackageReferencePropertyPrefix))
                .ToList();

            foreach(var value in existingFlexRefValues)
                value.Remove();
        }

        // Append new FlexRef entries at the end
        if(absentFlexReferencedProjects.Count > 0)
        {
            if(customBuildProperties == null)
            {
                customBuildProperties = new XElement("CustomBuildProperties");
                settingsElement.Add(customBuildProperties);
            }

            foreach(var flexReferencedProject in absentFlexReferencedProjects)
                customBuildProperties.Add(new XElement("Value", $"{flexReferencedProject.PropertyName} = true"));
        }

        // Remove CustomBuildProperties element entirely if it has no entries
        if(customBuildProperties is { HasElements: false })
            customBuildProperties.Remove();

        document.SaveWithoutDeclaration(file.FullName);
        Console.WriteLine($"  Updated: {file.FullName} ({absentFlexReferencedProjects.Count} absent package(s))");
    }
}
