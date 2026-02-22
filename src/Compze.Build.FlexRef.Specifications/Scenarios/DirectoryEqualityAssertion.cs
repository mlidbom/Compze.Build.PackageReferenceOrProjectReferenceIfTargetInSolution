using Compze.Utilities.Testing.Must;

namespace Compze.Build.FlexRef.Scenarios;

static class DirectoryEqualityAssertion
{
   public static void AssertContentsAreIdentical(DirectoryInfo expectedDirectory, DirectoryInfo actualDirectory)
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
