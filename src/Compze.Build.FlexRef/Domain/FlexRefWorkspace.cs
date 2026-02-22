namespace Compze.Build.FlexRef.Domain;

class FlexRefWorkspace
{
   public DirectoryInfo RootDirectory { get; }
   internal IReadOnlyList<ManagedProject> AllProjects { get; set; } = [];
   internal IReadOnlyList<FlexReferencedProject> FlexReferencedProjects { get; set; } = [];

   public FlexRefWorkspace(DirectoryInfo rootDirectory) =>
      RootDirectory = rootDirectory;

   bool ConfigurationExists => FlexRefConfigurationFile.ExistsIn(RootDirectory);

   void ScanProjects()
   {
      var allProjects = ManagedProject.ScanDirectory(RootDirectory);
      AllProjects = allProjects;

      foreach(var project in allProjects)
         project.Workspace = this;
   }

   void LoadConfigurationAndResolve()
   {
      if(!ConfigurationExists)
         throw new ConfigurationNotFoundException(RootDirectory);

      var configFile = new FlexRefConfigurationFile(RootDirectory);
      configFile.Load();

      FlexReferencedProjects = ManagedProject.ResolveFlexReferencedProjects(configFile, AllProjects.ToList());
   }

   public void Init()
   {
      ScanProjects();

      if(ConfigurationExists)
         throw new ConfigurationAlreadyExistsException(RootDirectory);

      new FlexRefConfigurationFile(RootDirectory).CreateDefault(AllProjects);
      FlexRefPropsFileWriter.WriteToDirectory(RootDirectory);
   }

   public void Sync()
   {
      ScanProjects();
      LoadConfigurationAndResolve();

      FlexRefPropsFileWriter.WriteToDirectory(RootDirectory);
      DirectoryBuildPropsFileUpdater.UpdateOrCreate(this);
      new CsprojUpdater(this).UpdateAll();
      new NCrunchUpdater(this).UpdateAll();
   }
}
