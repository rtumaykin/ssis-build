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
using System.IO;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Tests
{
    public class BuildArgumentsTests : IDisposable
    {
        private readonly string _workingFolder;
        private readonly string _oldWorkingFolder;
        
        public BuildArgumentsTests()
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
        public void Pass_Process_AllProperties_ExplicitProjectPathNoRoot()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            var protectionLevelString = new[] {"EncryptAllWithPassword", "EncryptSensitiveWithPassword"}[Fakes.RandomInt(0, 200) / 200];
            var args = new[]
            {
                Path.GetFileName(filePath),
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.ProtectionLevel)}",
                protectionLevelString,
                $"-{nameof(BuildArguments.NewPassword)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.Password)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.OutputFolder)}",
                $".\\{Fakes.RandomString()}",
                $"-{nameof(BuildArguments.ReleaseNotes)}",
                $".\\{Fakes.RandomString()}",
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
            };

            // Execute
            var buildArguments = new BuildArguments();
            buildArguments.ProcessArgs(args);

            // Assert
            Assert.NotNull(buildArguments);
            Assert.Equal(filePath, buildArguments.ProjectPath);
            Assert.Equal(args[2], buildArguments.Configuration);
            Assert.Equal(args[4], buildArguments.ProtectionLevel);
            Assert.Equal(args[6], buildArguments.NewPassword);
            Assert.Equal(args[8], buildArguments.Password);
            Assert.Equal(args[10], buildArguments.OutputFolder);
            Assert.Equal(args[12], buildArguments.ReleaseNotes);

            Assert.NotNull(buildArguments.Parameters);
            Assert.True(buildArguments.Parameters.Count == 2);
            Assert.Equal(args[14], buildArguments.Parameters[args[13].Split(new [] {':'}, 2)[1]]);
            Assert.Equal(args[16], buildArguments.Parameters[args[15].Split(new[] { ':' }, 2)[1]]);
        }

        [Fact]
        public void Pass_Process_AllProperties_ExplicitProjectPathWithRoot()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            var protectionLevelString = new[] { "EncryptAllWithPassword", "EncryptSensitiveWithPassword" }[Fakes.RandomInt(0, 200) / 200];
            var args = new[]
            {
                filePath,
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.ProtectionLevel)}",
                protectionLevelString,
                $"-{nameof(BuildArguments.NewPassword)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.Password)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.OutputFolder)}",
                $".\\{Fakes.RandomString()}",
                $"-{nameof(BuildArguments.ReleaseNotes)}",
                $".\\{Fakes.RandomString()}",
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
            };

            // Execute
            var buildArguments = new BuildArguments();
            buildArguments.ProcessArgs(args);

            // Assert
            Assert.NotNull(buildArguments);
            Assert.Equal(filePath, buildArguments.ProjectPath);
            Assert.Equal(args[2], buildArguments.Configuration);
            Assert.Equal(args[4], buildArguments.ProtectionLevel);
            Assert.Equal(args[6], buildArguments.NewPassword);
            Assert.Equal(args[8], buildArguments.Password);
            Assert.Equal(args[10], buildArguments.OutputFolder);
            Assert.Equal(args[12], buildArguments.ReleaseNotes);

            Assert.NotNull(buildArguments.Parameters);
            Assert.True(buildArguments.Parameters.Count == 2);
            Assert.Equal(args[14], buildArguments.Parameters[args[13].Split(new[] { ':' }, 2)[1]]);
            Assert.Equal(args[16], buildArguments.Parameters[args[15].Split(new[] { ':' }, 2)[1]]);
        }

        [Fact]
        public void Pass_Process_AllProperties_ImplicitProjectPath()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();
            var protectionLevelString = new[] { "EncryptAllWithPassword", "EncryptSensitiveWithPassword" }[Fakes.RandomInt(0, 200) / 200];
            var args = new[]
            {
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.ProtectionLevel)}",
                protectionLevelString,
                $"-{nameof(BuildArguments.NewPassword)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.Password)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.OutputFolder)}",
                $".\\{Fakes.RandomString()}",
                $"-{nameof(BuildArguments.ReleaseNotes)}",
                $".\\{Fakes.RandomString()}",
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
            };

            // Execute
            var buildArguments = new BuildArguments();
            buildArguments.ProcessArgs(args);

            // Assert
            Assert.NotNull(buildArguments);
            Assert.Equal(filePath, buildArguments.ProjectPath);
            Assert.Equal(args[1], buildArguments.Configuration);
            Assert.Equal(args[3], buildArguments.ProtectionLevel);
            Assert.Equal(args[5], buildArguments.NewPassword);
            Assert.Equal(args[7], buildArguments.Password);
            Assert.Equal(args[9], buildArguments.OutputFolder);
            Assert.Equal(args[11], buildArguments.ReleaseNotes);

            Assert.NotNull(buildArguments.Parameters);
            Assert.True(buildArguments.Parameters.Count == 2);
            Assert.Equal(args[13], buildArguments.Parameters[args[12].Split(new[] { ':' }, 2)[1]]);
            Assert.Equal(args[15], buildArguments.Parameters[args[14].Split(new[] { ':' }, 2)[1]]);
        }

        [Fact]
        public void Fail_Parse_NoArgumentValue()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();
            // need to set configuration value, otherwise the call will fail;
            var args = new[]
            {
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.Password)}"
            };
            var testException = new NoValueProvidedException(nameof(BuildArguments.Password));

            // Execute
            var exception = Record.Exception(() => new BuildArguments().ProcessArgs(args));


            // Assert
            Assert.NotNull(exception);
            Assert.IsType<NoValueProvidedException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Fail_Parse_InvalidToken()
        {
            // Setup
            var token = Fakes.RandomString();
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();
            // need to set configuration value, otherwise the call will fail;
            var args = new[]
            {
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                token
            };
            var testException = new InvalidTokenException(token);

            // Execute
            var exception = Record.Exception(() => new BuildArguments().ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidTokenException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Fail_Validate_NullProjectPath()
        {
            // Setup
            var buildArguments = new BuildArguments();
            var args = new[]
{
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
            };
            var testException = new ProjectFileNotFoundException(Environment.CurrentDirectory);

            // Execute
            var exception = Record.Exception(() => buildArguments.ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ProjectFileNotFoundException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Fail_Validate_NoConfiguration()
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();
            var buildArguments = new BuildArguments();
            var testException = new MissingRequiredArgumentException(nameof(buildArguments.Configuration));
            
            // Execute
            var exception = Record.Exception(() => buildArguments.ProcessArgs(new string[] { }));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MissingRequiredArgumentException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData("XYZ")]
        [InlineData("ServerStorage")]
        [InlineData("EncryptSensitiveWithUserKey")]
        public void Fail_Validate_InvalidProtectionLevel(string protectionLevelString)
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();

            var args = new[]
            {
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.ProtectionLevel)}",
                protectionLevelString,
                $"-{nameof(BuildArguments.NewPassword)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.Password)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.OutputFolder)}",
                $".\\{Fakes.RandomString()}",
                $"-{nameof(BuildArguments.ReleaseNotes)}",
                $".\\{Fakes.RandomString()}",
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
            };
            var testException = new InvalidArgumentException(nameof(BuildArguments.ProtectionLevel), protectionLevelString);

            // Execute
            var exception = Record.Exception(() => new BuildArguments().ProcessArgs(args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidArgumentException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData("DontSaveSensitive")]
        [InlineData("EncryptAllWithPassword")]
        [InlineData("EncryptSensitiveWithPassword")]
        public void Pass_Validate_ValidProtectionLevel(string protectionLevelString)
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();

            var args = new[]
            {
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.ProtectionLevel)}",
                protectionLevelString,
                $"-{nameof(BuildArguments.Password)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.OutputFolder)}",
                $".\\{Fakes.RandomString()}",
                $"-{nameof(BuildArguments.ReleaseNotes)}",
                $".\\{Fakes.RandomString()}",
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
            };

            // Execute
            var exception = Record.Exception(() => new BuildArguments().ProcessArgs(args));

            // Assert
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("DontSaveSensitive", true, false)]
        [InlineData("DontSaveSensitive", false, false)]
        [InlineData("EncryptAllWithPassword", false, true)]
        [InlineData("EncryptAllWithPassword", true, true)]
        [InlineData("EncryptAllWithPassword", true, false)]
        [InlineData("EncryptSensitiveWithPassword", false, true)]
        [InlineData("EncryptSensitiveWithPassword", true, true)]
        [InlineData("EncryptSensitiveWithPassword", true, false)]
        public void Pass_Validate_ProtectionLevelPasswordCombination(string protectionLevelString, bool password, bool newPassword)
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();

            var argsList = new List<string>
            {
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.ProtectionLevel)}",
                protectionLevelString,
                $"-{nameof(BuildArguments.OutputFolder)}",
                $".\\{Fakes.RandomString()}",
                $"-{nameof(BuildArguments.ReleaseNotes)}",
                $".\\{Fakes.RandomString()}",
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
            };

            if (password)
            {
                argsList.AddRange(new[]
                {
                    $"-{nameof(BuildArguments.Password)}",
                    Fakes.RandomString(),
                });
            }

            if (newPassword)
            {
                argsList.AddRange(new[]
                {
                    $"-{nameof(BuildArguments.NewPassword)}",
                    Fakes.RandomString(),
                });
            }


            // Execute
            var exception = Record.Exception(() => new BuildArguments().ProcessArgs(argsList.ToArray()));

            // Assert
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("EncryptAllWithPassword")]
        [InlineData("EncryptSensitiveWithPassword")]
        public void Fail_Validate_ProtectionLevelPasswordCombination(string protectionLevelString)
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();

            var argsList = new List<string>
            {
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.ProtectionLevel)}",
                protectionLevelString,
                $"-{nameof(BuildArguments.OutputFolder)}",
                $".\\{Fakes.RandomString()}",
                $"-{nameof(BuildArguments.ReleaseNotes)}",
                $".\\{Fakes.RandomString()}",
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
            };
            var testException = new PasswordRequiredException(protectionLevelString);


            // Execute
            var exception = Record.Exception(() => new BuildArguments().ProcessArgs(argsList.ToArray()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<PasswordRequiredException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData("DontSaveSensitive", true, true)]
        [InlineData("DontSaveSensitive", false, true)]
        public void Fail_Validate_ProtectionLevelDontSaveSensitiveWithPassword(string protectionLevelString, bool password, bool newPassword)
        {
            // Setup
            var filePath = Path.Combine(_workingFolder, $"{Fakes.RandomString()}.dtproj");
            File.Create(filePath).Close();

            var argsList = new List<string>
            {
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                $"-{nameof(BuildArguments.ProtectionLevel)}",
                protectionLevelString,
                $"-{nameof(BuildArguments.OutputFolder)}",
                $".\\{Fakes.RandomString()}",
                $"-{nameof(BuildArguments.ReleaseNotes)}",
                $".\\{Fakes.RandomString()}",
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
                $"-Parameter:{Fakes.RandomString()}::{Fakes.RandomString()}",
                Fakes.RandomString(),
            };

            if (password)
            {
                argsList.AddRange(new[]
                {
                    $"-{nameof(BuildArguments.Password)}",
                    Fakes.RandomString(),
                });
            }

            if (newPassword)
            {
                argsList.AddRange(new[]
                {
                    $"-{nameof(BuildArguments.NewPassword)}",
                    Fakes.RandomString(),
                });
            }
            var testException = new DontSaveSensitiveWithPasswordException();


            // Execute
            var exception = Record.Exception(() => new BuildArguments().ProcessArgs(argsList.ToArray()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<DontSaveSensitiveWithPasswordException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Fail_ProcessArgs_NullArgs()
        {
            //Setup
            var buildArguments = new BuildArguments();

            // Execute
            var exception = Record.Exception(() => buildArguments.ProcessArgs(null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<NullReferenceException>(exception);

        }
    }
}
