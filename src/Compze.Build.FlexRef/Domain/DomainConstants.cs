namespace Compze.Build.FlexRef.Domain;

static class DomainConstants
{
    public const string ConfigurationFileName = "FlexRef.config.xml";
    public const string PropsFileName = "FlexRef.props";
    public const string BuildDirectoryName = "build";
    public const string DirectoryBuildPropsFileName = "Directory.Build.props";
    public const string UsePackageReferencePropertyPrefix = "UsePackageReference_";
    public const string NCrunchSolutionFileExtension = ".v3.ncrunchsolution";
    public const string CsprojFileExtension = ".csproj";
    public const string CsprojSearchPattern = "*.csproj";
    public const string SlnxSearchPattern = "*.slnx";
    public static readonly string[] DirectoriesToSkip = ["bin", "obj", "node_modules", ".git", ".vs", ".idea"];
}
