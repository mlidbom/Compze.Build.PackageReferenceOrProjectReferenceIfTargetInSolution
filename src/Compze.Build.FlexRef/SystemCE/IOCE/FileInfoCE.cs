namespace Compze.Build.FlexRef.SystemCE.IOCE;

static class FileInfoCE
{
    public static string ComputeRelativePathWithBackslashes(this FileInfo @this, FileInfo target)
    {
        var fromDirectory = @this.DirectoryName!;
        var relativePath = Path.GetRelativePath(fromDirectory, target.FullName);
        return relativePath.Replace('/', '\\');
    }
}
