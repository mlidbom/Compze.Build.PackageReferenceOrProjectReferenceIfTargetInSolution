using Compze.Utilities.Testing.Must;
using Xunit;

namespace Compze.Build.FlexRef.Specifications;

static class ScenarioRunner
{
   public static TheoryData<string> DiscoverScenarios(string scenariosDirectoryPath)
   {
      var theoryData = new TheoryData<string>();
      foreach(var directory in Directory.GetDirectories(scenariosDirectoryPath).Order())
         theoryData.Add(Path.GetFileName(directory));
      return theoryData;
   }

   public static void RunAndVerify(string scenarioDirectoryPath, Action<Domain.FlexRefWorkspace> command)
   {
      var startStatePath = Path.Combine(scenarioDirectoryPath, "start-state");
      var expectedStatePath = Path.Combine(scenarioDirectoryPath, "expected-state");
      var tempWorkFolderPath = Path.Combine(scenarioDirectoryPath, "temp-work-folder");

      if(Directory.Exists(tempWorkFolderPath))
         Directory.Delete(tempWorkFolderPath, true);

      CopyDirectory(startStatePath, tempWorkFolderPath);

      var workspace = new Domain.FlexRefWorkspace(new DirectoryInfo(tempWorkFolderPath));
      command(workspace);

      CompareDirectoryContents(expectedStatePath, tempWorkFolderPath);
   }

   static void CopyDirectory(string sourcePath, string destinationPath)
   {
      foreach(var filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
      {
         var relativePath = Path.GetRelativePath(sourcePath, filePath);
         var destinationFilePath = Path.Combine(destinationPath, relativePath);
         Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);
         File.Copy(filePath, destinationFilePath);
      }
   }

   static void CompareDirectoryContents(string expectedDirectoryPath, string actualDirectoryPath)
   {
      var expectedRelativeFilePaths = GetSortedRelativeFilePaths(expectedDirectoryPath);
      var actualRelativeFilePaths = GetSortedRelativeFilePaths(actualDirectoryPath);

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
         var expectedContent = NormalizeLineEndings(File.ReadAllText(Path.Combine(expectedDirectoryPath, relativeFilePath)));
         var actualContent = NormalizeLineEndings(File.ReadAllText(Path.Combine(actualDirectoryPath, relativeFilePath)));

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

   static SortedSet<string> GetSortedRelativeFilePaths(string directoryPath) =>
      new(Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                   .Select(filePath => Path.GetRelativePath(directoryPath, filePath).Replace('\\', '/')),
         StringComparer.OrdinalIgnoreCase);

   static string NormalizeLineEndings(string content) => content.Replace("\r\n", "\n");
}
