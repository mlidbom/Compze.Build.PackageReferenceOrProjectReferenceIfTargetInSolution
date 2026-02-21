namespace Compze.Build.FlexRef;

class FlexReference
{
    public string PackageId { get; }
    public FileInfo CsprojFile { get; }
    public string PropertyName { get; }

    public FlexReference(ManagedProject project)
    {
        PackageId = project.PackageId ?? throw new ArgumentException("Project must have a PackageId.", nameof(project));
        CsprojFile = project.CsprojFile;
        PropertyName = "UsePackageReference_" + PackageId.Replace('.', '_').Replace('-', '_');
    }
}
