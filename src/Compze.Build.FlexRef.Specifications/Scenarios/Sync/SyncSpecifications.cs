using System.Runtime.CompilerServices;
using Xunit;

namespace Compze.Build.FlexRef.Scenarios.Sync;

public class SyncSpecifications
{
   static string ThisFilePath([CallerFilePath] string? path = null) => path!;
   static readonly DirectoryInfo ScenariosDirectory = new(Path.Combine(Path.GetDirectoryName(ThisFilePath())!, "SyncScenarios"));

   public static TheoryData<string> Scenarios => ScenarioRunner.DiscoverScenarios(ScenariosDirectory);

   [Theory]
   [MemberData(nameof(Scenarios))]
   public void produces_expected_workspace_state(string scenarioName)
   {
      ScenarioRunner.RunAndVerify(
         new DirectoryInfo(Path.Combine(ScenariosDirectory.FullName, scenarioName)),
         workspace => workspace.Sync(),
         verifyIdempotency: true);
   }
}
