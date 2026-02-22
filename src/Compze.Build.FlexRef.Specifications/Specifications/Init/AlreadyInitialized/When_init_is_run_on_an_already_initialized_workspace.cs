using System.Runtime.CompilerServices;
using Compze.Build.FlexRef.Domain;
using Compze.Build.FlexRef.Domain.Exceptions;
using Compze.Utilities.Testing.Must;
using Xunit;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Build.FlexRef.Specifications.Init.AlreadyInitialized;

public class When_init_is_run_on_an_already_initialized_workspace
{
   static string ThisFilePath([CallerFilePath] string? path = null) => path!;
   static readonly DirectoryInfo WorkspaceDirectory = new(Path.Combine(Path.GetDirectoryName(ThisFilePath())!, "workspace"));

   [Fact]
   public void throws_ConfigurationAlreadyExistsException()
   {
      var workspace = new FlexRefWorkspace(WorkspaceDirectory);
      Invoking(() => workspace.Init()).Must().Throw<ConfigurationAlreadyExistsException>();
   }
}
