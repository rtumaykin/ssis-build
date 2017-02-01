namespace SsisBuild.Core
{
    public interface IProjectManifest : IProjectFile
    {
        string[] ConnectionManagerNames { get; }
        string Description { get; set; }
        string[] PackageNames { get; }
        ProtectionLevel ProtectonLevel { get; }
        string VersionBuild { get; set; }
        string VersionComments { get; set; }
        string VersionMajor { get; set; }
        string VersionMinor { get; set; }
    }
}