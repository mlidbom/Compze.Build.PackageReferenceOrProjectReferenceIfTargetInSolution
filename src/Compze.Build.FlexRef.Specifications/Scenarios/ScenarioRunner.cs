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

      DirectoryEqualityAssertion.AssertContentsAreIdentical(expectedState, tempWorkFolder);
      FlexReferenceResolutionAssertion.AssertMsBuildResolvesReferencesBasedOnSolutionMembership(tempWorkFolder);

      if(verifyIdempotency)
      {
         var workspaceForSecondRun = new Domain.FlexRefWorkspace(tempWorkFolder);
         command(workspaceForSecondRun);

         DirectoryEqualityAssertion.AssertContentsAreIdentical(expectedState, tempWorkFolder);
         FlexReferenceResolutionAssertion.AssertMsBuildResolvesReferencesBasedOnSolutionMembership(tempWorkFolder);
      }
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
}
