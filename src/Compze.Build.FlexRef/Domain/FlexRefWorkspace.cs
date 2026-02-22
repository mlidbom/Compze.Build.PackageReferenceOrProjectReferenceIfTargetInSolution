using Compze.Build.FlexRef.Domain.Exceptions;

namespace Compze.Build.FlexRef.Domain;

class FlexRefWorkspace
{
   public DirectoryInfo RootDirectory { get; }
   internal IReadOnlyList<ManagedProject> AllProjects { get; set; } = [];
   internal IReadOnlyList<FlexReferencedProject> FlexReferencedProjects { get; set; } = [];

   FlexRefConfigurationFile ConfigurationFile { get; }

   public FlexRefWorkspace(DirectoryInfo rootDirectory)
   {
      if(!rootDirectory.Exists)
         throw new RootDirectoryNotFoundException(rootDirectory);

      RootDirectory = rootDirectory;
      ConfigurationFile = new FlexRefConfigurationFile(this);
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

      FlexReferencedProjects = ManagedProject.ResolveFlexReferencedProjects(ConfigurationFile, AllProjects.ToList());
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
      DirectoryBuildPropsFileUpdater.UpdateOrCreate(this);
      new CsprojUpdater(this).UpdateAll();

      foreach(var solution in SlnxSolution.FindAndParseAllSolutions(this))
         solution.UpdateNCrunchFile();
   }
}
