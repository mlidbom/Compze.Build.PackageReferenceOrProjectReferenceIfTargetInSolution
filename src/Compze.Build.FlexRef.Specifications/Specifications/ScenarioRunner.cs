using Compze.Utilities.Testing.Must;
using Xunit;

namespace Compze.Build.FlexRef.Specifications;

static class ScenarioRunner
{
   public static TheoryData<string> DiscoverScenarios(DirectoryInfo scenariosDirectory)
   {
      var theoryData = new TheoryData<string>();
      foreach(var subdirectory in scenariosDirectory.GetDirectories().OrderBy(directory => directory.Name))
         theoryData.Add(subdirectory.Name);
      return theoryData;
   }

   public static void RunAndVerify(DirectoryInfo scenarioDirectory, Action<Domain.FlexRefWorkspace> command)
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
   }

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
