using System.Runtime.CompilerServices;
using Compze.Build.FlexRef.Domain;
using Compze.Build.FlexRef.Domain.Exceptions;
using Compze.Utilities.Testing.Must;
using Xunit;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Build.FlexRef.Specifications.Sync.NotInitialized;

public class When_sync_is_run_on_a_workspace_without_configuration
{
   static string ThisFilePath([CallerFilePath] string? path = null) => path!;
   static readonly DirectoryInfo WorkspaceDirectory = new(Path.Combine(Path.GetDirectoryName(ThisFilePath())!, "workspace"));

   [Fact]
   public void throws_ConfigurationNotFoundException()
   {
      var workspace = new FlexRefWorkspace(WorkspaceDirectory);
      Invoking(() => workspace.Sync()).Must().Throw<ConfigurationNotFoundException>();
   }
}
