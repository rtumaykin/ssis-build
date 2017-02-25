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
using Moq;
using SsisBuild.Core.Deployer;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class SsisDeployPowershellTests
    {
        private readonly Mock<IDeployer> _deployerMock;

        public SsisDeployPowershellTests()
        {
            _deployerMock = new Mock<IDeployer>();
        }

        [Fact]
        public void Pass_ProcessRecord()
        {
            // Setup
            var workingFolder = Fakes.RandomString();
            var deploymentFilePath = Fakes.RandomString();
            var serverInstance = Fakes.RandomString();
            var catalog = Fakes.RandomString();
            var folder = Fakes.RandomString();
            var projectName = Fakes.RandomString();
            var projectPassword = Fakes.RandomString();
            var eraseSensitiveInfo = Fakes.RandomBool();

            IDeployArguments deployArguments = null;

            _deployerMock.Setup(d => d.Deploy(It.IsAny<IDeployArguments>())).Callback((IDeployArguments da) => { deployArguments = da; });

            // Execute
            var powershellCmd = new SsisDeployPowershell
            {
                DeploymentFilePath = deploymentFilePath,
                Folder = folder,
                Catalog = catalog,
                ServerInstance = serverInstance,
                ProjectName = projectName,
                EraseSensitiveInfo = eraseSensitiveInfo,
                ProjectPassword = projectPassword
            };

            powershellCmd.ProcessRecordInternal(_deployerMock.Object, workingFolder);

            // Assert
            Assert.Equal(workingFolder, deployArguments.WorkingFolder);
            Assert.Equal(deploymentFilePath, deployArguments.DeploymentFilePath);
            Assert.Equal(serverInstance, deployArguments.ServerInstance);
            Assert.Equal(catalog, deployArguments.Catalog);
            Assert.Equal(folder, deployArguments.Folder);
            Assert.Equal(projectName, deployArguments.ProjectName);
            Assert.Equal(projectPassword, deployArguments.ProjectPassword);
            Assert.Equal(eraseSensitiveInfo, deployArguments.EraseSensitiveInfo);
        }

        [Fact]
        public void Fail_ProcessRecord()
        {

            // Setup
            var workingFolder = Fakes.RandomString();
            var deploymentFilePath = Fakes.RandomString();
            var serverInstance = Fakes.RandomString();
            var catalog = Fakes.RandomString();
            var folder = Fakes.RandomString();
            var projectName = Fakes.RandomString();
            var projectPassword = Fakes.RandomString();
            var eraseSensitiveInfo = Fakes.RandomBool();

            IDeployArguments deployArguments = null;

            _deployerMock.Setup(d => d.Deploy(It.IsAny<IDeployArguments>())).Throws(new Exception("TEST"));

            // Execute
            var powershellCmd = new SsisDeployPowershell
            {
                DeploymentFilePath = deploymentFilePath,
                Folder = folder,
                Catalog = catalog,
                ServerInstance = serverInstance,
                ProjectName = projectName,
                EraseSensitiveInfo = eraseSensitiveInfo,
                ProjectPassword = projectPassword
            };

            // Execute
            var exception = Record.Exception(() => powershellCmd.ProcessRecordInternal(_deployerMock.Object, workingFolder));

            Assert.NotNull(exception);
            Assert.IsType<Exception>(exception);
            Assert.Equal("TEST", exception.Message);

        }
    }
}
