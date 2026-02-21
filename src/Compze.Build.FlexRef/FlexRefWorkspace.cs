namespace Compze.Build.FlexRef;

class FlexRefWorkspace
{
   public DirectoryInfo RootDirectory { get; }
   public IReadOnlyList<ManagedProject> AllProjects { get; }
   public IReadOnlyList<FlexReference> FlexReferences { get; }

   internal FlexRefWorkspace(DirectoryInfo rootDirectory, List<ManagedProject> allProjects, List<FlexReference> flexReferences)
   {
      RootDirectory = rootDirectory;
      AllProjects = allProjects;
      FlexReferences = flexReferences;
   }

   public static FlexRefWorkspace ScanAndResolve(DirectoryInfo rootDirectory, FlexRefConfigurationFile configuration)
   {
      var allProjects = ManagedProject.ScanDirectory(rootDirectory);
      var flexReferences = ManagedProject.ResolveFlexReferences(configuration, allProjects);
      return new FlexRefWorkspace(rootDirectory, allProjects, flexReferences);
   }

   public void UpdateDirectoryBuildProps() => DirectoryBuildPropsFileUpdater.UpdateOrCreate(this);

   public void UpdateCsprojFiles()
   {
      var updater = new CsprojUpdater(this);
      foreach(var project in AllProjects)
         updater.UpdateIfNeeded(project);
   }

   public void UpdateNCrunchFiles()
   {
      var updater = new NCrunchUpdater(this);
      var solutions = SlnxSolution.FindAndParseAllSolutions(RootDirectory);
      foreach(var solution in solutions)
         updater.UpdateOrCreate(solution);
   }
}
