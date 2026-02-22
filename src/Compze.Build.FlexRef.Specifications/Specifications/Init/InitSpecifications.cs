using System.Runtime.CompilerServices;
using Xunit;

namespace Compze.Build.FlexRef.Specifications.Init;

public class InitSpecifications
{
   static string ThisFilePath([CallerFilePath] string? path = null) => path!;
   static readonly DirectoryInfo ScenariosDirectory = new(Path.Combine(Path.GetDirectoryName(ThisFilePath())!, "InitScenarios"));

   public static TheoryData<string> Scenarios => ScenarioRunner.DiscoverScenarios(ScenariosDirectory);

   [Theory]
   [MemberData(nameof(Scenarios))]
   public void produces_expected_workspace_state(string scenarioName)
   {
      ScenarioRunner.RunAndVerify(
         new DirectoryInfo(Path.Combine(ScenariosDirectory.FullName, scenarioName)),
         workspace => workspace.Init());
   }
}
