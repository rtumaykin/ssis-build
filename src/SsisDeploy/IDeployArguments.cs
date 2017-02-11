namespace SsisDeploy
{
    public interface IDeployArguments
    {
        string DeploymentFilePath { get; }
        string ServerInstance { get; }
        string Catalog { get; }
        string Folder { get; }
        string ProjectName { get; }
        bool EraseSensitiveInfo { get; }
        string ProjectPassword { get; }
        void ProcessArgs(string[] args);
    }
}