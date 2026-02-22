using Compze.Build.FlexRef.Domain.Exceptions;

namespace Compze.Build.FlexRef.Domain;

class FlexRefWorkspace
{
   public DirectoryInfo RootDirectory { get; }
   internal IReadOnlyList<CSProj> AllProjects { get; private set; } = [];
   internal IReadOnlyList<FlexReferencedProject> FlexReferencedProjects { get; private set; } = [];

   internal FlexRefConfigurationFile ConfigurationFile { get; }
   internal DirectoryBuildPropsFile DirectoryBuildPropsFile { get; }

   public FlexRefWorkspace(DirectoryInfo rootDirectory)
   {
      if(!rootDirectory.Exists)
         throw new RootDirectoryNotFoundException(rootDirectory);

      RootDirectory = rootDirectory;
      ConfigurationFile = new FlexRefConfigurationFile(this);
      DirectoryBuildPropsFile = new DirectoryBuildPropsFile(this);
   }

   void ScanProjects()
   {
      var allProjects = CSProj.ScanDirectory(this);
      AllProjects = allProjects;
   }

   void LoadConfigurationAndResolve()
   {
      if(!ConfigurationFile.Exists())
         throw new ConfigurationNotFoundException(RootDirectory);

      ConfigurationFile.Load();

      FlexReferencedProjects = CSProj.ResolveFlexReferencedProjects(this);
   }

   public void Init()
   {
      ScanProjects();

      if(ConfigurationFile.Exists())
         throw new ConfigurationAlreadyExistsException(RootDirectory);

      ConfigurationFile.CreateDefault();
      FlexRefPropsFile.Write(this);
   }

   public void Sync()
   {
      ScanProjects();
      LoadConfigurationAndResolve();

      FlexRefPropsFile.Write(this);
      DirectoryBuildPropsFile.UpdateOrCreate();
      CSProj.UpdateAll(this);

      foreach(var solution in SlnxSolution.FindAndParseAllSolutions(this))
         solution.UpdateNCrunchFile();
   }
}
