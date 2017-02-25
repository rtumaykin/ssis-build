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
        [Parameter(
            Position = 0,
            HelpMessage =
                "Full path to a SSIS deployment file (with ispac extension). If a project file is not specified, ssisdeploy searches current working directory, for a file with ispac extension and uses that file."
        )]
        public string DeploymentFilePath { get; set; }

        [Parameter(Mandatory = true)]
        public string ServerInstance { get; set; }

        [Parameter]
        public string Catalog { get; set; }

        [Parameter(Mandatory = true)]
        public string Folder { get; set; }

        [Parameter]
        public string ProjectName { get; set; }

        [Parameter]
        public string ProjectPassword { get; set; }

        [Parameter]
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
