namespace Compze.Build.FlexRef.Domain;

class FlexReferencedProject
{
    public string PackageId { get; }
    public FileInfo CsprojFile { get; }
    public string PropertyName { get; }

    public FlexReferencedProject(ManagedProject project)
    {
        PackageId = project.PackageId ?? throw new ArgumentException("Project must have a PackageId.", nameof(project));
        CsprojFile = project.CsprojFile;
        PropertyName = DomainConstants.UsePackageReferencePropertyPrefix + PackageId.Replace('.', '_').Replace('-', '_');
    }
}
