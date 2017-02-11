namespace SsisDeploy
{
    public interface IDeployer
    {
        void Deploy(IDeployArguments deployArguments);
    }
}