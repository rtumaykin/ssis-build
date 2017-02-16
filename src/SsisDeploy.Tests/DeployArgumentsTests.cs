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
using SsisBuild.Core;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisDeploy.Tests
{
    public class DeployArgumentsTests : IDisposable
    {
        private readonly string _workingFolder;
        private readonly string _oldWorkingFolder;

        public DeployArgumentsTests()
        {
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingFolder);
            _oldWorkingFolder = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _workingFolder;
        }

        public void Dispose()
        {
            Environment.CurrentDirectory = _oldWorkingFolder;
            Directory.Delete(_workingFolder, true);
        }

        [Fact]
        public void Pass_New()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.ispac");
            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}"
            };
            var deployArguments = new DeployArguments();

            // Execute
            deployArguments.ProcessArgs(args);

            // Assert
            Assert.Equal(args[0], deployArguments.DeploymentFilePath);
            Assert.Equal(args[2], deployArguments.ServerInstance);
            Assert.Equal(args[4], deployArguments.Catalog);
            Assert.Equal(args[6], deployArguments.Folder);
            Assert.Equal(args[8], deployArguments.ProjectName);
            Assert.Equal(args[10], deployArguments.ProjectPassword);
            Assert.Equal(true, deployArguments.EraseSensitiveInfo);
        }

        [Fact]
        public void Pass_New_ImplicitDeploymentFile()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.ispac");
            File.Create(filePath).Close();

            var args = new[]
            {
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}"
            };
            var deployArguments = new DeployArguments();

            // Execute
            deployArguments.ProcessArgs(args);

            // Assert
            Assert.Equal(filePath, deployArguments.DeploymentFilePath);
            Assert.Equal(args[1], deployArguments.ServerInstance);
            Assert.Equal(args[3], deployArguments.Catalog);
            Assert.Equal(args[5], deployArguments.Folder);
            Assert.Equal(args[7], deployArguments.ProjectName);
            Assert.Equal(args[9], deployArguments.ProjectPassword);
            Assert.Equal(true, deployArguments.EraseSensitiveInfo);
        }

        [Fact]
        public void Pass_New_ReverseOrder()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, "test.ispac");

            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}",
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            deployArguments.ProcessArgs(args);

            // Assert
            Assert.Equal(args[0], deployArguments.DeploymentFilePath);
            Assert.Equal(args[5], deployArguments.ServerInstance);
            Assert.Equal(args[3], deployArguments.Catalog);
            Assert.Equal(args[11], deployArguments.Folder);
            Assert.Equal(args[7], deployArguments.ProjectName);
            Assert.Equal(args[9], deployArguments.ProjectPassword);
            Assert.Equal(true, deployArguments.EraseSensitiveInfo);
        }

        [Fact]
        public void Pass_New_NoSensitiveInfoFlag()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, "test.ispac");

            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            deployArguments.ProcessArgs(args);

            // Assert
            Assert.Equal(args[0], deployArguments.DeploymentFilePath);
            Assert.Equal(args[4], deployArguments.ServerInstance);
            Assert.Equal(args[2], deployArguments.Catalog);
            Assert.Equal(args[10], deployArguments.Folder);
            Assert.Equal(args[6], deployArguments.ProjectName);
            Assert.Equal(args[8], deployArguments.ProjectPassword);
            Assert.Equal(false, deployArguments.EraseSensitiveInfo);
        }

        [Fact]
        public void Fail_New_InvalidToken()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, "test.ispac");

            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}",
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                "-ServerInstanceXX",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            var exception = Record.Exception(() => deployArguments.ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidTokenException>(exception);
        }

        [Fact]
        public void Fail_New_MissingServerInstance()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, "test.ispac");

            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}",
                //$"-{nameof(DeployArguments.ServerInstance)}",
                //Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            var exception = Record.Exception(() => deployArguments.ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.True(((MissingRequiredArgumentException) exception).RequiredArgument == nameof(deployArguments.ServerInstance));
        }

        [Fact]
        public void Fail_New_MissingCatalog()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, "test.ispac");

            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}",
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                //$"-{nameof(DeployArguments.Catalog)}",
                //Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            var exception = Record.Exception(() => deployArguments.ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.True(((MissingRequiredArgumentException) exception).RequiredArgument == nameof(deployArguments.Catalog));
        }

        [Fact]
        public void Fail_New_MissingProjectName()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, "test.ispac");

            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}",
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                //$"-{nameof(DeployArguments.ProjectName)}",
                //Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            var exception = Record.Exception(() => deployArguments.ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.True(((MissingRequiredArgumentException) exception).RequiredArgument == nameof(deployArguments.ProjectName));
        }

        [Fact]
        public void Fail_New_MissingFolder()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, "test.ispac");

            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}",
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                //Fakes.RandomString(),
                //$"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            var exception = Record.Exception(() => deployArguments.ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.True(((MissingRequiredArgumentException) exception).RequiredArgument == nameof(deployArguments.Folder));
        }

        [Fact]
        public void Fail_New_InvalidExtension()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, "test.invaid");

            var args = new[]
            {
                filePath,
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}",
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                //Fakes.RandomString(),
                //$"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            var exception = Record.Exception(() => deployArguments.ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidExtensionException>(exception);
        }

        [Fact]
        public void Fail_New_NoImplicitFileFound()
        {
            // Setup
            var args = new[]
            {
                $"-{nameof(DeployArguments.EraseSensitiveInfo)}",
                $"-{nameof(DeployArguments.ServerInstance)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.Catalog)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectName)}",
                Fakes.RandomString(),
                $"-{nameof(DeployArguments.ProjectPassword)}",
                //Fakes.RandomString(),
                //$"-{nameof(DeployArguments.Folder)}",
                Fakes.RandomString(),
            };
            var deployArguments = new DeployArguments();

            // Execute
            var exception = Record.Exception(() => deployArguments.ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<DeploymentFileNotFoundException>(exception);
        }
    }
}
