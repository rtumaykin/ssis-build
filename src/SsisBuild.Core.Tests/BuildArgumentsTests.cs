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
using System.Collections.Generic;
using SsisBuild.Core.Builder;
using SsisBuild.Core.ProjectManagement;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class BuildArgumentsTests
    {
        [Fact]
        public void Pass_Process_AllProperties()
        {
            // Setup
            var projectPath = Fakes.RandomString();
            var workingFolder = Fakes.RandomString();
            var protectionLevelString = new[] {nameof(ProtectionLevel.EncryptAllWithPassword), nameof(ProtectionLevel.EncryptSensitiveWithPassword)}[Fakes.RandomInt(0, 200) / 200];
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

            // Execute
            var buildArguments = new BuildArguments(workingFolder, projectPath, outputFolder, protectionLevelString, password, newPassword, configuration, releaseNotes, parameters);

            // Assert
            Assert.Equal(projectPath, buildArguments.ProjectPath);
            Assert.Equal(configuration, buildArguments.Configuration);
            Assert.Equal(protectionLevelString, buildArguments.ProtectionLevel);
            Assert.Equal(newPassword, buildArguments.NewPassword);
            Assert.Equal(password, buildArguments.Password);
            Assert.Equal(outputFolder, buildArguments.OutputFolder);
            Assert.Equal(releaseNotes, buildArguments.ReleaseNotes);
            Assert.Equal(workingFolder, buildArguments.WorkingFolder);

            Assert.NotNull(buildArguments.Parameters);
            Assert.Equal(parametersCount, buildArguments.Parameters.Count);

            foreach (var parameter in parameters)
            {
                Assert.True(buildArguments.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(parameter.Value, buildArguments.Parameters[parameter.Key]);
            }
        }

  
        [Fact]
        public void Fail_Validate_NoConfiguration()
        {
            // Setup
            
            // Execute
            var exception = Record.Exception(() => new BuildArguments(null, null, null, null, null, null, null, null, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.Equal(((MissingRequiredArgumentException) exception).MissingArgument, nameof(BuildArguments.Configuration));
        }

        [Theory]
        [InlineData("XYZ")]
        [InlineData(nameof(ProtectionLevel.ServerStorage))]
        [InlineData(nameof(ProtectionLevel.EncryptSensitiveWithUserKey))]
        public void Fail_Validate_InvalidProtectionLevel(string protectionLevelString)
        {
            // Setup
            var testException = new InvalidArgumentException(nameof(BuildArguments.ProtectionLevel), protectionLevelString);

            // Execute
            var exception = Record.Exception(() => new BuildArguments(null, null, null, protectionLevelString, null, null, Fakes.RandomString(), null, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidArgumentException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData(nameof(ProtectionLevel.DontSaveSensitive))]
        [InlineData(nameof(ProtectionLevel.EncryptAllWithPassword))]
        [InlineData(nameof(ProtectionLevel.EncryptSensitiveWithPassword))]
        public void Pass_Validate_ValidProtectionLevel(string protectionLevelString)
        {
            // Setup

            // Execute
            var exception = Record.Exception(() => new BuildArguments(null, null, null, protectionLevelString, Fakes.RandomString(), null, Fakes.RandomString(), null, null));

            // Assert
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(nameof(ProtectionLevel.DontSaveSensitive), true, false)]
        [InlineData(nameof(ProtectionLevel.DontSaveSensitive), false, false)]
        [InlineData(nameof(ProtectionLevel.EncryptAllWithPassword), false, true)]
        [InlineData(nameof(ProtectionLevel.EncryptAllWithPassword), true, true)]
        [InlineData(nameof(ProtectionLevel.EncryptAllWithPassword), true, false)]
        [InlineData(nameof(ProtectionLevel.EncryptSensitiveWithPassword), false, true)]
        [InlineData(nameof(ProtectionLevel.EncryptSensitiveWithPassword), true, true)]
        [InlineData(nameof(ProtectionLevel.EncryptSensitiveWithPassword), true, false)]
        public void Pass_Validate_ProtectionLevelPasswordCombination(string protectionLevelString, bool password, bool newPassword)
        {
            // Setup

            // Execute
            var exception =
                Record.Exception(
                    () =>
                        new BuildArguments(null, null, null, protectionLevelString, password ? Fakes.RandomString() : null, newPassword ? Fakes.RandomString() : null,
                            Fakes.RandomString(), null, null));

            // Assert
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(nameof(ProtectionLevel.EncryptAllWithPassword))]
        [InlineData(nameof(ProtectionLevel.EncryptSensitiveWithPassword))]
        public void Fail_Validate_ProtectionLevelPasswordCombination(string protectionLevelString)
        {
            // Setup
            var testException = new PasswordRequiredException(protectionLevelString);


            // Execute
            var exception = Record.Exception(() => new BuildArguments(null, null, null, protectionLevelString, null, null, Fakes.RandomString(), null, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<PasswordRequiredException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData(nameof(ProtectionLevel.DontSaveSensitive), true, true)]
        [InlineData(nameof(ProtectionLevel.DontSaveSensitive), false, true)]
        public void Fail_Validate_ProtectionLevelDontSaveSensitiveWithPassword(string protectionLevelString, bool password, bool newPassword)
        {
            // Setup
            var testException = new DontSaveSensitiveWithPasswordException();


            // Execute
            var exception =
                Record.Exception(
                    () =>
                        new BuildArguments(null, null, null, protectionLevelString, password ? Fakes.RandomString() : null, newPassword ? Fakes.RandomString() : null,
                            Fakes.RandomString(), null, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<DontSaveSensitiveWithPasswordException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
