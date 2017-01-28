namespace SsisBuild.Core
{
    public interface IProjectFactory
    {
        Project LoadFromIspac(string filePath, string password);
        Project LoadFromDtproj(string filePath, string configurationName, string password);
    }
}