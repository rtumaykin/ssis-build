//-----------------------------------------------------------------------
//   Copyright 2017 Roman Tumaykin
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Management.Automation;

namespace SsisBuild.Core.Deployer
{
    [Cmdlet(VerbsData.Publish, "SsisDeploymentPackage")]
    public class SsisDeployPowershell : PSCmdlet
    {
        [Parameter(HelpMessage = "Full path to an SSIS deployment file (with ispac extension). If a deployment file is not specified, " +
                                 "ssisdeploy searches current working directory for a file with ispac extension and uses that file.",
                   Position = 0
        )]
        public string DeploymentFilePath { get; set; }

        [Parameter(HelpMessage= "Required. Full Name of the target SQL Server instance.",
                   Mandatory = true)]
        public string ServerInstance { get; set; }

        [Parameter(HelpMessage = "Name of the SSIS Catalog on the target server. If not supplied, then SSISDB value is used.")]
        public string Catalog { get; set; }

        [Parameter(HelpMessage = "Required. Deployment folder within destination catalog.",
                   Mandatory = true)]
        public string Folder { get; set; }

        [Parameter(HelpMessage = "Name of the project in the destination folder. If not supplied, then deployment file name is used.")]
        public string ProjectName { get; set; }

        [Parameter(HelpMessage = "Password to decrypt sensitive data for deployment.")]
        public string ProjectPassword { get; set; }

        [Parameter(HelpMessage = "Option to remove all sensitive info from the deployment ispac and deploy all sensitive parameters separately. " +
                                 "If not specified then sensitive data will not be removed.")]
        public SwitchParameter EraseSensitiveInfo { get; set; }

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
                string.IsNullOrWhiteSpace(_workingFolder) ? null : _workingFolder,
                string.IsNullOrWhiteSpace(DeploymentFilePath) ? null : DeploymentFilePath,
                string.IsNullOrWhiteSpace(ServerInstance) ? null : ServerInstance,
                string.IsNullOrWhiteSpace(Catalog) ? null : Catalog,
                string.IsNullOrWhiteSpace(Folder) ? null : Folder,
                string.IsNullOrWhiteSpace(ProjectName) ? null : ProjectName,
                string.IsNullOrWhiteSpace(ProjectPassword) ? null : ProjectPassword, 
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
