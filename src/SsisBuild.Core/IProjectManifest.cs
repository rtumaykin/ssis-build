namespace SsisBuild.Core
{
    public interface IProjectManifest : IProjectFile
    {
        string[] ConnectionManagerNames { get; }
        string Description { get; set; }
        string[] PackageNames { get; }
        int VersionBuild { get; set; }
        string VersionComments { get; set; }
        int VersionMajor { get; set; }
        int VersionMinor { get; set; }
    }
}