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
using System.IO;
using Moq;
using SsisBuild.Core.Deployer;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisDeploy.Tests
{
    public class SsisDeployTests
    {
        private readonly Mock<IDeployer> _deployerMock;

        public SsisDeployTests()
        {
            _deployerMock = new Mock<IDeployer>();
        }

        [Fact]
        public void Pass_MainInternal()
        {
            // Setup
            var args = new[]
            {
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ServerInstance)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}"
            };

            IDeployArguments deployArguments = null;
            _deployerMock.Setup(d => d.Deploy(It.IsAny<IDeployArguments>())).Callback((IDeployArguments da) => deployArguments = da);

            // Execute
            Program.MainInternal(_deployerMock.Object, args);

            // Assert
            Assert.NotNull(deployArguments);
            Assert.Equal(Environment.CurrentDirectory, deployArguments.WorkingFolder);
            Assert.Equal(args[0], deployArguments.DeploymentFilePath);
            Assert.Equal(args[2], deployArguments.ServerInstance);
            Assert.Equal(args[4], deployArguments.Catalog);
            Assert.Equal(args[6], deployArguments.ProjectName);
            Assert.Equal(args[8], deployArguments.Folder);
            Assert.Equal(args[10], deployArguments.ProjectPassword);
            Assert.True(deployArguments.EraseSensitiveInfo);
        }

        [Fact]
        public void Fail_MainInternal_InvalidTokenException()
        {
            // Setup
            var stdOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_deployerMock.Object, new[]
            {
                $"-{Fakes.RandomString()}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.ServerInstance)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}", Fakes.RandomString(),
            }));

            Console.SetOut(stdOut);

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidTokenException>(exception);
            Assert.True(consoleOutput.ToString().ToLowerInvariant().Contains("usage"));
        }

        [Fact]
        public void Fail_MainInternal_InvalidTokenException_NoDash()
        {
            // Setup
            var stdOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_deployerMock.Object, new[]
            {
                $"{Fakes.RandomString()}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.ServerInstance)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}", Fakes.RandomString(),
            }));

            Console.SetOut(stdOut);

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidTokenException>(exception);
            Assert.True(consoleOutput.ToString().ToLowerInvariant().Contains("usage"));
        }

        [Fact]
        public void Fail_MainInternal_MissingRequiredArgumentException()
        {
            // Setup
            var stdOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_deployerMock.Object, new[]
            {
                $"-{nameof(DeployArguments.Catalog)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}", Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}", Fakes.RandomString(),
            }));

            Console.SetOut(stdOut);

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.True(consoleOutput.ToString().Contains(new MissingRequiredArgumentException(nameof(DeployArguments.ServerInstance)).Message));
            Assert.True(consoleOutput.ToString().ToLowerInvariant().Contains("usage"));
        }

        [Fact]
        public void Fail_ProcessArgs_NullArgs()
        {
            //Setup
            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_deployerMock.Object, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<NullReferenceException>(exception);

        }
    }
}
