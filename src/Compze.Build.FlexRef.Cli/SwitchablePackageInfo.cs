namespace Compze.Build.FlexRef.Cli;

record SwitchablePackageInfo(
    string PackageId,
    string CsprojFileName,
    string CsprojFullPath)
{
    public string PropertyName { get; } = "UsePackageReference_" + PackageId.Replace('.', '_').Replace('-', '_');
}
