namespace Compze.Build.FlexRef.Domain;

class ConfigurationAlreadyExistsException(DirectoryInfo rootDirectory)
    : Exception($"Configuration already exists: {Path.Combine(rootDirectory.FullName, DomainConstants.ConfigurationFileName)}")
{
    public DirectoryInfo RootDirectory { get; } = rootDirectory;
}

class ConfigurationNotFoundException(DirectoryInfo rootDirectory)
    : Exception($"Configuration not found: {Path.Combine(rootDirectory.FullName, DomainConstants.ConfigurationFileName)}")
{
    public DirectoryInfo RootDirectory { get; } = rootDirectory;
}

class RootDirectoryNotFoundException(DirectoryInfo rootDirectory)
    : Exception($"Root directory not found: {rootDirectory.FullName}")
{
    public DirectoryInfo RootDirectory { get; } = rootDirectory;
}
