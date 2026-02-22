using Compze.Build.FlexRef.Domain.Exceptions;

namespace Compze.Build.FlexRef.Domain;

class FlexRefWorkspace
{
   public DirectoryInfo RootDirectory { get; }
   internal IReadOnlyList<ManagedProject> AllProjects { get; set; } = [];
   internal IReadOnlyList<FlexReferencedProject> FlexReferencedProjects { get; set; } = [];

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
      var allProjects = ManagedProject.ScanDirectory(this);
      AllProjects = allProjects;
   }

   void LoadConfigurationAndResolve()
   {
      if(!ConfigurationFile.Exists())
         throw new ConfigurationNotFoundException(RootDirectory);

      ConfigurationFile.Load();

      FlexReferencedProjects = ManagedProject.ResolveFlexReferencedProjects(this);
   }

   public void Init()
   {
      ScanProjects();

      if(ConfigurationFile.Exists())
         throw new ConfigurationAlreadyExistsException(RootDirectory);

      ConfigurationFile.CreateDefault();
      FlexRefPropsFileWriter.Write(this);
   }

   public void Sync()
   {
      ScanProjects();
      LoadConfigurationAndResolve();

      FlexRefPropsFileWriter.Write(this);
      DirectoryBuildPropsFile.UpdateOrCreate();
      new CsprojUpdater(this).UpdateAll();

      foreach(var solution in SlnxSolution.FindAndParseAllSolutions(this))
         solution.UpdateNCrunchFile();
   }
}
