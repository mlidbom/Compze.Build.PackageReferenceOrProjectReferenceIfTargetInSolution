namespace Compze.Build.FlexRef.Domain;

class FlexRefWorkspace
{
   public DirectoryInfo RootDirectory { get; }
   public IReadOnlyList<ManagedProject> AllProjects { get; }
   public IReadOnlyList<FlexReferencedProject> FlexReferencedProjects { get; }

   FlexRefWorkspace(DirectoryInfo rootDirectory, List<ManagedProject> allProjects, List<FlexReferencedProject> flexReferencedProjects)
   {
      RootDirectory = rootDirectory;
      AllProjects = allProjects;
      FlexReferencedProjects = flexReferencedProjects;

      foreach(var project in allProjects)
         project.Workspace = this;
   }

   public bool ConfigurationExists => FlexRefConfigurationFile.ExistsIn(RootDirectory);

   public static FlexRefWorkspace Scan(DirectoryInfo rootDirectory)
   {
      var allProjects = ManagedProject.ScanDirectory(rootDirectory);
      return new FlexRefWorkspace(rootDirectory, allProjects, []);
   }

   public static FlexRefWorkspace ScanAndResolve(DirectoryInfo rootDirectory)
   {
      if(!FlexRefConfigurationFile.ExistsIn(rootDirectory))
         throw new ConfigurationNotFoundException(rootDirectory);

      var configFile = new FlexRefConfigurationFile(rootDirectory);
      configFile.Load();

      var allProjects = ManagedProject.ScanDirectory(rootDirectory);
      var flexReferencedProjects = ManagedProject.ResolveFlexReferencedProjects(configFile, allProjects);
      return new FlexRefWorkspace(rootDirectory, allProjects, flexReferencedProjects);
   }

   public void CreateDefaultConfiguration()
   {
      if(ConfigurationExists)
         throw new ConfigurationAlreadyExistsException(RootDirectory);

      new FlexRefConfigurationFile(RootDirectory).CreateDefault(AllProjects);
   }

   public static void Init(DirectoryInfo rootDirectory)
   {
      var workspace = Scan(rootDirectory);
      workspace.CreateDefaultConfiguration();
      workspace.WriteFlexRefProps();
   }

   public static void Sync(DirectoryInfo rootDirectory)
   {
      var workspace = ScanAndResolve(rootDirectory);
      workspace.WriteFlexRefProps();
      workspace.UpdateDirectoryBuildProps();
      workspace.UpdateCsprojFiles();
      workspace.UpdateNCrunchFiles();
   }

   public void WriteFlexRefProps() =>
      FlexRefPropsFileWriter.WriteToDirectory(RootDirectory);

   public void UpdateDirectoryBuildProps() => DirectoryBuildPropsFileUpdater.UpdateOrCreate(this);

   public void UpdateCsprojFiles() => new CsprojUpdater(this).UpdateAll();

   public void UpdateNCrunchFiles() => new NCrunchUpdater(this).UpdateAll();
}
