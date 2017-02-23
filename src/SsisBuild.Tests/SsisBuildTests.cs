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
using SsisBuild.Core.Builder;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Tests
{
    public class SsisBuildTests
    {
        private readonly Mock<IBuilder> _builderMock;

        public SsisBuildTests()
        {
            _builderMock = new Mock<IBuilder>();
        }

        [Fact]
        public void Fail_Main_BuildArgumentsValidationException()
        {
            // Setup
            var fakeValue = Fakes.RandomString();
            var testException = new InvalidArgumentException($"{nameof(BuildArguments.ProtectionLevel)}", fakeValue);

            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_builderMock.Object, new[] { $"-{nameof(BuildArguments.ProtectionLevel)}", fakeValue }));

            // Assert
            Assert.IsType<InvalidArgumentException>(exception);
            Assert.Equal(testException.Message, exception.Message, StringComparer.InvariantCultureIgnoreCase);
        }


        [Fact]
        public void Pass_Main_NoException()
        {
            // Setup

            // Execute
            Program.MainInternal(_builderMock.Object, new[] { $"-{nameof(BuildArguments.Configuration)}", Fakes.RandomString() });

            // Assert
        }

        [Fact]
        public void Pass_Process_AllProperties_ExplicitProjectPathNoRoot()
        {
            // Setup
            var filePath = $"{Fakes.RandomString()}.dtproj";
            var protectionLevelString = new[] { "EncryptAllWithPassword", "EncryptSensitiveWithPassword" }[Fakes.RandomInt(0, 199) / 200];
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

            IBuildArguments buildArguments = null;

            _builderMock.Setup(b => b.Build(It.IsAny<IBuildArguments>())).Callback((IBuildArguments ba) => buildArguments = ba);

            // Execute
            Program.MainInternal(_builderMock.Object, args);

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
        public void Fail_Validate_NoArgumentValue()
        {
            // Setup
            var args = new[]
            {
                $"-{nameof(BuildArguments.Password)}"
            };
            var testException = new NoValueProvidedException(nameof(BuildArguments.Password));

            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_builderMock.Object, args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<NoValueProvidedException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Fail_Parse_InvalidToken_NoDash()
        {
            // Setup
            var token = Fakes.RandomString();
            var args = new[]
            {
                // so that it does not get confused with ProjectPath
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                token
            };
            var testException = new InvalidTokenException(token);

            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_builderMock.Object, args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidTokenException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Fail_Parse_InvalidToken()
        {
            // Setup
            var token = $"-{Fakes.RandomString()}";
            var args = new[]
            {
                // so that it does not get confused with ProjectPath
                $"-{nameof(BuildArguments.Configuration)}",
                Fakes.RandomString(),
                token
            };
            var testException = new InvalidTokenException(token);

            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_builderMock.Object, args));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidTokenException>(exception);
            Assert.Equal(exception.Message, testException.Message, StringComparer.InvariantCultureIgnoreCase);
        }


        [Fact]
        public void Fail_ProcessArgs_NullArgs()
        {
            //Setup
            // Execute
            var exception = Record.Exception(() => Program.MainInternal(_builderMock.Object, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<NullReferenceException>(exception);

        }
    }
}
