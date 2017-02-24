using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using SsisBuild.Core.Builder;

namespace SsisBuild.Core.Deployer
{
    [Cmdlet(VerbsData.Publish, "SsisDeploymentPackage")]
    public class SsisDeployPowershell : PSCmdlet
    {
        [Parameter(
            Position = 0,
            HelpMessage =
                "Full path to a SSIS deployment file (with ispac extension). If a project file is not specified, ssisdeploy searches current working directory, for a file with ispac extension and uses that file."
        )]
        public string DeploymentFilePath { get; set; }

        [Parameter(Mandatory = true)]
        public string ServerInstance { get; set; }

        [Parameter(Mandatory = true)]
        public string Catalog { get; set; }

        [Parameter(Mandatory = true)]
        public string Folder { get; set; }

        [Parameter(Mandatory = true)]
        public string ProjectName { get; set; }

        [Parameter]
        public string ProjectPassword { get; set; }

        [Parameter]
        public bool EraseSensitiveInfo { get; set; }

        private IDeployer _deployer;
        private string _workingFolder;

        internal void ProcessRecordInternal(IDeployer deployer, string workingFolder)
        {
            _deployer = deployer;
            _workingFolder = workingFolder;
            ProcessRecord();
        }

        protected override void ProcessRecord()
        {
            _workingFolder = _workingFolder ?? CurrentProviderLocation("FileSystem").ProviderPath;

            var deployArguments = new DeployArguments(
                _workingFolder, 
                DeploymentFilePath, 
                ServerInstance, 
                Catalog, 
                Folder, 
                ProjectName, 
                ProjectPassword, 
                EraseSensitiveInfo
            );

            _deployer = _deployer ?? new Deployer();

            try
            {
                _deployer.Deploy(deployArguments);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
