namespace Compze.Build.FlexRef;

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

   public static FlexRefWorkspace ScanAndResolve(DirectoryInfo rootDirectory, FlexRefConfigurationFile configuration)
   {
      var allProjects = ManagedProject.ScanDirectory(rootDirectory);
      var flexReferencedProjects = ManagedProject.ResolveFlexReferencedProjects(configuration, allProjects);
      return new FlexRefWorkspace(rootDirectory, allProjects, flexReferencedProjects);
   }

   public void UpdateDirectoryBuildProps() => DirectoryBuildPropsFileUpdater.UpdateOrCreate(this);

   public void UpdateCsprojFiles() => new CsprojUpdater(this).UpdateAll();

   public void UpdateNCrunchFiles() => new NCrunchUpdater(this).UpdateAll();
}
