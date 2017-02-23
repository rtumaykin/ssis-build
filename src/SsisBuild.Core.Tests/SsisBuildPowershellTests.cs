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
using System.Collections;
using System.Collections.Generic;
using Moq;
using SsisBuild.Core.Builder;
using SsisBuild.Core.ProjectManagement;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class SsisBuildPowershellTests
    {
        private readonly Mock<IBuilder> _builder;

        public SsisBuildPowershellTests()
        {
            _builder = new Mock<IBuilder>();
        }

        [Fact]
        public void Pass_ProcessRecord()
        {
            // Setup
            var projectPath = Fakes.RandomString();
            var workingFolder = Fakes.RandomString();
            var protectionLevelString = new[] { nameof(ProtectionLevel.EncryptAllWithPassword), nameof(ProtectionLevel.EncryptSensitiveWithPassword) }[Fakes.RandomInt(0, 199) / 200];
            var configuration = Fakes.RandomString();
            var password = Fakes.RandomString();
            var newPassword = Fakes.RandomString();
            var outputFolder = Fakes.RandomString();
            var releaseNotes = Fakes.RandomString();

            var parametersCount = Fakes.RandomInt(0, 10);
            var parameters = new Dictionary<string, string>();
            for (var i = 0; i < parametersCount; i++)
            {
                parameters.Add(Fakes.RandomString(), Fakes.RandomString());
            }

            BuildArguments buildArguments = null;

            _builder.Setup(b => b.Build(It.IsAny<IBuildArguments>())).Callback((IBuildArguments ba) => { buildArguments = ba as BuildArguments; });

            // Execute
            var powershellCmd = new SsisBuildPowershell
            {
                ProtectionLevel = protectionLevelString,
                Configuration = configuration,
                NewPassword = newPassword,
                OutputFolder = outputFolder,
                Parameters = new Hashtable(parameters),
                Password = password,
                ProjectPath = projectPath,
                ReleaseNotes = releaseNotes
            };

            powershellCmd.ProcessRecordInternal(_builder.Object, workingFolder);

            // Assert
            Assert.NotNull(buildArguments);
            Assert.Equal(projectPath, buildArguments.ProjectPath);
            Assert.Equal(workingFolder, buildArguments.WorkingFolder);
            Assert.Equal(protectionLevelString, buildArguments.ProtectionLevel);
            Assert.Equal(configuration, buildArguments.Configuration);
            Assert.Equal(password, buildArguments.Password);
            Assert.Equal(newPassword, buildArguments.NewPassword);
            Assert.Equal(outputFolder, buildArguments.OutputFolder);
            Assert.Equal(releaseNotes, buildArguments.ReleaseNotes);

            Assert.NotNull(buildArguments.Parameters);
            Assert.Equal(parametersCount, buildArguments.Parameters.Count);

            foreach (var parameter in parameters)
            {
                Assert.True(buildArguments.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(parameter.Value, buildArguments.Parameters[parameter.Key]);
            }
        }

        [Fact]
        public void Fail_ProcessRecord()
        {
            // Setup
            var projectPath = Fakes.RandomString();
            var workingFolder = Fakes.RandomString();
            var protectionLevelString = new[] { nameof(ProtectionLevel.EncryptAllWithPassword), nameof(ProtectionLevel.EncryptSensitiveWithPassword) }[Fakes.RandomInt(0, 199) / 200];
            var configuration = Fakes.RandomString();
            var password = Fakes.RandomString();
            var newPassword = Fakes.RandomString();
            var outputFolder = Fakes.RandomString();
            var releaseNotes = Fakes.RandomString();

            var parametersCount = Fakes.RandomInt(0, 10);
            var parameters = new Dictionary<string, string>();
            for (var i = 0; i < parametersCount; i++)
            {
                parameters.Add(Fakes.RandomString(), Fakes.RandomString());
            }

            _builder.Setup(b => b.Build(It.IsAny<IBuildArguments>())).Throws(new Exception("TEST"));
            var powershellCmd = new SsisBuildPowershell
            {
                ProtectionLevel = protectionLevelString,
                Configuration = configuration,
                NewPassword = newPassword,
                OutputFolder = outputFolder,
                Parameters = new Hashtable(parameters),
                Password = password,
                ProjectPath = projectPath,
                ReleaseNotes = releaseNotes
            };

            // Execute
            var exception = Record.Exception(() => powershellCmd.ProcessRecordInternal(_builder.Object, Fakes.RandomString()));

            Assert.NotNull(exception);
            Assert.IsType<Exception>(exception);
            Assert.Equal("TEST", exception.Message);

        }
    }
}
