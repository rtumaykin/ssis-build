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

using SsisBuild.Core.Deployer;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class DeployArgumentsTests
    {
        [Fact]
        public void Pass_New_AllArguments()
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


            // Execute
            var deployArguments = new DeployArguments(workingFolder, deploymentFilePath, serverInstance, catalog, folder, projectName, projectPassword, eraseSensitiveInfo);

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
        public void Fail_New_MissingServerInstance()
        {
            // Setup

            // Execute
            var exception = Record.Exception(() => new DeployArguments(null, null, null, Fakes.RandomString(), Fakes.RandomString(), Fakes.RandomString(), null, Fakes.RandomBool()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.True(((MissingRequiredArgumentException) exception).MissingArgument == nameof(DeployArguments.ServerInstance));
        }

        [Fact]
        public void Fail_New_MissingFolder()
        {
            // Setup

            // Execute
            var exception = Record.Exception(() => new DeployArguments(null, null, Fakes.RandomString(), Fakes.RandomString(), null, Fakes.RandomString(), null, Fakes.RandomBool()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.True(((MissingRequiredArgumentException)exception).MissingArgument == nameof(DeployArguments.Folder));
        }
    }
}
