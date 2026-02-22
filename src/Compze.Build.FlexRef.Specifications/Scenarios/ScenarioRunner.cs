using System.Xml.Linq;
using Compze.Build.FlexRef.Domain;
using Compze.Utilities.Testing.Must;
using Microsoft.Build.Evaluation;
using Xunit;

namespace Compze.Build.FlexRef.Scenarios;

static class ScenarioRunner
{
   public static TheoryData<string> DiscoverScenarios(DirectoryInfo scenariosDirectory)
   {
      var theoryData = new TheoryData<string>();
      foreach(var subdirectory in scenariosDirectory.GetDirectories().OrderBy(directory => directory.Name))
         theoryData.Add(subdirectory.Name);
      return theoryData;
   }

   public static void RunAndVerify(DirectoryInfo scenarioDirectory, Action<Domain.FlexRefWorkspace> command, bool verifyIdempotency = false)
   {
      var startState = new DirectoryInfo(Path.Combine(scenarioDirectory.FullName, "start-state"));
      var expectedState = new DirectoryInfo(Path.Combine(scenarioDirectory.FullName, "expected-state"));
      var tempWorkFolder = new DirectoryInfo(Path.Combine(scenarioDirectory.FullName, "temp-work-folder"));

      if(tempWorkFolder.Exists)
         tempWorkFolder.Delete(true);

      CopyDirectory(startState, tempWorkFolder);

      var workspace = new Domain.FlexRefWorkspace(tempWorkFolder);
      command(workspace);

      CompareDirectoryContents(expectedState, tempWorkFolder);
      VerifyMSBuildReferenceResolution(tempWorkFolder);

      if(verifyIdempotency)
      {
         var workspaceForSecondRun = new Domain.FlexRefWorkspace(tempWorkFolder);
         command(workspaceForSecondRun);

         CompareDirectoryContents(expectedState, tempWorkFolder);
         VerifyMSBuildReferenceResolution(tempWorkFolder);
      }
   }

   static void VerifyMSBuildReferenceResolution(DirectoryInfo workspaceDirectory)
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

   static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
   {
      foreach(var sourceFile in source.GetFiles("*", SearchOption.AllDirectories))
      {
         var relativePath = Path.GetRelativePath(source.FullName, sourceFile.FullName);
         var destinationFile = new FileInfo(Path.Combine(destination.FullName, relativePath));
         destinationFile.Directory!.Create();
         sourceFile.CopyTo(destinationFile.FullName);
      }
   }

   static void CompareDirectoryContents(DirectoryInfo expectedDirectory, DirectoryInfo actualDirectory)
   {
      var expectedRelativeFilePaths = GetSortedRelativeFilePaths(expectedDirectory);
      var actualRelativeFilePaths = GetSortedRelativeFilePaths(actualDirectory);

      var missingFiles = expectedRelativeFilePaths.Except(actualRelativeFilePaths).ToList();
      var extraFiles = actualRelativeFilePaths.Except(expectedRelativeFilePaths).ToList();

      if(missingFiles.Count > 0 || extraFiles.Count > 0)
      {
         var message = "Directory file sets differ:";
         if(missingFiles.Count > 0)
            message += $"\n  Missing from actual: {string.Join(", ", missingFiles)}";
         if(extraFiles.Count > 0)
            message += $"\n  Extra in actual: {string.Join(", ", extraFiles)}";
         throw new AssertionFailedException(message);
      }

      foreach(var relativeFilePath in expectedRelativeFilePaths)
      {
         var expectedContent = NormalizeLineEndings(new FileInfo(Path.Combine(expectedDirectory.FullName, relativeFilePath)).OpenText().ReadToEnd());
         var actualContent = NormalizeLineEndings(new FileInfo(Path.Combine(actualDirectory.FullName, relativeFilePath)).OpenText().ReadToEnd());

         try
         {
            actualContent.Must().Be(expectedContent);
         }
         catch(AssertionFailedException exception)
         {
            throw new AssertionFailedException($"File content mismatch: {relativeFilePath}", exception);
         }
      }
   }

   static SortedSet<string> GetSortedRelativeFilePaths(DirectoryInfo directory) =>
      new(directory.GetFiles("*", SearchOption.AllDirectories)
                   .Select(file => Path.GetRelativePath(directory.FullName, file.FullName).Replace('\\', '/')),
         StringComparer.OrdinalIgnoreCase);

   static string NormalizeLineEndings(string content) => content.Replace("\r\n", "\n");
}
