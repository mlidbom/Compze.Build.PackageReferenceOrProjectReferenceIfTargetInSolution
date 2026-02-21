namespace Compze.Build.FlexRef.Cli;

record DiscoveredProjectReference(string IncludePath, string ResolvedFileName);

record DiscoveredPackageReference(string PackageName, string Version);

record DiscoveredProject(
    string CsprojFullPath,
    string CsprojFileName,
    string? PackageId,
    bool IsPackable,
    List<DiscoveredProjectReference> ProjectReferences,
    List<DiscoveredPackageReference> PackageReferences);
