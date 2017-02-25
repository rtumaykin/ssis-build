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

namespace SsisBuild.Core.Deployer
{
    public class DeployArguments : IDeployArguments
    {
        public DeployArguments(string workingFolder, string deploymentFilePath, string serverInstance, string catalog, string folder, string projectName, string projectPassword, bool eraseSensitiveInfo)
        {
            DeploymentFilePath = deploymentFilePath;
            ServerInstance = serverInstance;
            Catalog = catalog;
            Folder = folder;
            ProjectName = projectName;
            ProjectPassword = projectPassword;
            EraseSensitiveInfo = eraseSensitiveInfo;
            WorkingFolder = workingFolder;

            Validate();
        }
        public string WorkingFolder { get; }

        public string DeploymentFilePath { get; }

        public string ServerInstance { get; }

        public string Catalog { get; }

        public string Folder { get; }

        public string ProjectName { get; }

        public bool EraseSensitiveInfo { get; }

        public string ProjectPassword { get; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ServerInstance))
                throw new MissingRequiredArgumentException(nameof(ServerInstance));

            if (string.IsNullOrWhiteSpace(Folder))
                throw new MissingRequiredArgumentException(nameof(Folder));
        }
    }
}
