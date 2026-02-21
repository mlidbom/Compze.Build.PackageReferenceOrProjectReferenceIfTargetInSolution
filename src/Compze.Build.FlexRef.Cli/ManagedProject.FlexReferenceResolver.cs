namespace Compze.Build.FlexRef.Cli;

partial class ManagedProject
{
    static class FlexReferenceResolver
    {
        public static List<FlexReference> Resolve(FlexRefConfigurationFile configuration, List<ManagedProject> allProjects)
        {
            var packableProjects = allProjects
                                  .Where(project => project is { IsPackable: true, PackageId: not null })
                                  .ToList();

            var resolvedPackages = new List<FlexReference>();

            if(configuration.UseAutoDiscover)
            {
                foreach(var project in packableProjects)
                {
                    if(configuration.AutoDiscoverExclusions
                                    .Any(exclusion => exclusion.EqualsIgnoreCase(project.PackageId!)))
                        continue;

                    resolvedPackages.Add(new FlexReference(project));
                }
            }

            foreach(var explicitPackageName in configuration.ExplicitPackageNames)
            {
                if(resolvedPackages.Any(existing =>
                                            existing.PackageId.EqualsIgnoreCase(explicitPackageName)))
                    continue;

                var matchingProject = packableProjects
                   .FirstOrDefault(project =>
                                       project.PackageId!.EqualsIgnoreCase(explicitPackageName));

                if(matchingProject != null)
                {
                    resolvedPackages.Add(new FlexReference(matchingProject));
                } else
                {
                    Console.Error.WriteLine(
                        $"  Warning: Explicit package '{explicitPackageName}' was not found in any project.");
                }
            }

            foreach(var package in resolvedPackages)
            {
                var expectedFileName = package.PackageId + ".csproj";
                if(!package.CsprojFile.Name.EqualsIgnoreCase(expectedFileName))
                {
                    Console.Error.WriteLine(
                        $"  Warning: Package '{package.PackageId}' is in project file '{package.CsprojFile.Name}' (expected '{expectedFileName}')");
                }
            }

            return resolvedPackages
                  .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
                  .ToList();
        }
    }
}
