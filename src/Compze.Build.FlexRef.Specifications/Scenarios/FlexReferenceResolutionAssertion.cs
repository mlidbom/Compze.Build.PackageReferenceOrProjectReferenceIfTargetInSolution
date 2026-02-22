using System.Xml.Linq;
using Compze.Build.FlexRef.Domain;
using Compze.Utilities.Testing.Must;
using Microsoft.Build.Evaluation;

namespace Compze.Build.FlexRef.Scenarios;

static class FlexReferenceResolutionAssertion
{
   public static void AssertMsBuildResolvesReferencesBasedOnSolutionMembership(DirectoryInfo workspaceDirectory)
   {
      var flexRefPropsFile = new FileInfo(Path.Combine(workspaceDirectory.FullName, DomainConstants.BuildDirectoryName, DomainConstants.PropsFileName));
      if(!flexRefPropsFile.Exists)
         return;

      var slnxFiles = workspaceDirectory.GetFiles(DomainConstants.SlnxSearchPattern, SearchOption.AllDirectories);
      var csprojFiles = workspaceDirectory.GetFiles(DomainConstants.CsprojSearchPattern, SearchOption.AllDirectories);

      foreach(var slnxFile in slnxFiles)
      {
         var projectFileNamesInSolution = ParseSlnxProjectFileNames(slnxFile);

         using var projectCollection = new ProjectCollection();
         var globalProperties = new Dictionary<string, string> { ["SolutionPath"] = slnxFile.FullName };

         foreach(var csprojFile in csprojFiles)
         {
            var msbuildProject = new Project(csprojFile.FullName, globalProperties, null, projectCollection);

            var projectReferences = msbuildProject.GetItems("ProjectReference")
                                                  .Select(item => Path.GetFileName(item.EvaluatedInclude))
                                                  .ToList();

            var packageReferences = msbuildProject.GetItems("PackageReference")
                                                  .Select(item => item.EvaluatedInclude)
                                                  .ToList();

            var flexReferencedPackageIds = FindFlexReferencedPackageIds(msbuildProject);

            foreach(var packageId in flexReferencedPackageIds)
            {
               var expectedCsprojFileName = packageId + DomainConstants.CsprojFileExtension;
               var projectIsInSolution = projectFileNamesInSolution.Contains(expectedCsprojFileName, StringComparer.OrdinalIgnoreCase);

               var hasProjectReference = projectReferences.Any(fileName => fileName.Equals(expectedCsprojFileName, StringComparison.OrdinalIgnoreCase));
               var hasPackageReference = packageReferences.Any(name => name.Equals(packageId, StringComparison.OrdinalIgnoreCase));

               if(!hasProjectReference && !hasPackageReference)
                  continue; // This project doesn't consume this package (e.g. it IS the package itself)

               var context = $"[{slnxFile.Name} → {csprojFile.Name} → {packageId}]";

               if(projectIsInSolution)
               {
                  if(!hasProjectReference)
                     throw new AssertionFailedException($"{context} Expected ProjectReference to {expectedCsprojFileName} (project is in solution) but found none.");
                  if(hasPackageReference)
                     throw new AssertionFailedException($"{context} Found unexpected PackageReference to {packageId} (project is in solution — should be ProjectReference).");
               }
               else
               {
                  if(!hasPackageReference)
                     throw new AssertionFailedException($"{context} Expected PackageReference to {packageId} (project is absent from solution) but found none.");
                  if(hasProjectReference)
                     throw new AssertionFailedException($"{context} Found unexpected ProjectReference to {expectedCsprojFileName} (project is absent from solution — should be PackageReference).");
               }
            }
         }
      }
   }

   static HashSet<string> ParseSlnxProjectFileNames(FileInfo slnxFile) =>
      XDocument.Load(slnxFile.FullName)
               .Descendants("Project")
               .Select(element => element.Attribute("Path")?.Value)
               .Where(path => path != null)
               .Select(path => Path.GetFileName(path!))
               .ToHashSet(StringComparer.OrdinalIgnoreCase);

   static List<string> FindFlexReferencedPackageIds(Project msbuildProject) =>
      msbuildProject.Properties
                    .Where(property => property.Name.StartsWith(DomainConstants.UsePackageReferencePropertyPrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(property => property.Name[DomainConstants.UsePackageReferencePropertyPrefix.Length..].Replace('_', '.'))
                    .ToList();
}
