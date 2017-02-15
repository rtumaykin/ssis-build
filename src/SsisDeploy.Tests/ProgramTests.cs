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
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisDeploy.Tests
{
    public class ProgramTests
    {
        private readonly Mock<IDeployer> _deployerMock;
        private readonly Mock<IDeployArguments> _deployArgsMock;

        public ProgramTests()
        {
            _deployerMock = new Mock<IDeployer>();
            _deployArgsMock = new Mock<IDeployArguments>();
        }

        [Fact]
        public void Pass_MainInternal()
        {
            // Setup

            // Execute
            var result = Program.MainInternal(_deployerMock.Object, _deployArgsMock.Object, new string []{});

            // Assert
            Assert.NotNull(result);
            Assert.True(result);
        }

        [Fact]
        public void Fail_MainInternal_Usage()
        {
            // Setup
            var stdOut = Console.Out;
            var message = Fakes.RandomString();
            _deployArgsMock.Setup(a => a.ProcessArgs(It.IsAny<string[]>())).Throws(new MissingRequiredArgumentException(message));
            var consoleOutput = new StringWriter();
            bool? result;
            try
            {
                Console.SetOut(consoleOutput);

                // Execute
                result = Program.MainInternal(_deployerMock.Object, _deployArgsMock.Object, new string[] { });
            }
            finally
            {
                Console.SetOut(stdOut);
            }

            // Assert
            Assert.True(consoleOutput.ToString().Contains("Usage"));
            Assert.True(consoleOutput.ToString().Contains(message));
            Assert.NotNull(result);
            Assert.False(result);
        }

        [Fact]
        public void Fail_MainInternal_OtherException()
        {
            // Setup
            var stdOut = Console.Out;
            var message = Fakes.RandomString();
            _deployArgsMock.Setup(a => a.ProcessArgs(It.IsAny<string[]>())).Throws(new Exception(message));
            var consoleOutput = new StringWriter();
            bool? result;
            try
            {
                Console.SetOut(consoleOutput);

                // Execute
                result = Program.MainInternal(_deployerMock.Object, _deployArgsMock.Object, new string[] { });
            }
            finally
            {
                Console.SetOut(stdOut);
            }

            // Assert
            Assert.True(consoleOutput.ToString().Contains(message));
            Assert.NotNull(result);
            Assert.False(result);
        }
    }
}
