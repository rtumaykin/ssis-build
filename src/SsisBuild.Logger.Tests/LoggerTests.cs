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
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Logger.Tests
{
    public class LoggerTests
    {
        [Fact]
        public void Pass_Message()
        {
            // Setup
            var stdOut = Console.Out;
            var consoleOutput = new StringWriter();
            var message = Fakes.RandomString();
            var logger = new ConsoleLogger();

            // Execute
            try
            {
                Console.SetOut(consoleOutput);
                logger.LogMessage(message);

            }
            finally
            {
                Console.SetOut(stdOut);
            }

            // Assert
            Assert.True(consoleOutput.ToString().Contains(message));
        }

        [Fact]
        public void Pass_Warning()
        {
            // Setup
            var stdOut = Console.Out;
            var consoleOutput = new StringWriter();
            var message = Fakes.RandomString();
            var logger = new ConsoleLogger();

            // Execute
            try
            {
                Console.SetOut(consoleOutput);
                logger.LogWarning(message);

            }
            finally
            {
                Console.SetOut(stdOut);
            }

            // Assert
            Assert.True(consoleOutput.ToString().Contains(message));
        }

        [Fact]
        public void Pass_Error()
        {
            // Setup
            var stdOut = Console.Out;
            var consoleOutput = new StringWriter();
            var message = Fakes.RandomString();
            var logger = new ConsoleLogger();

            // Execute
            try
            {
                Console.SetOut(consoleOutput);
                logger.LogError(message);

            }
            finally
            {
                Console.SetOut(stdOut);
            }

            // Assert
            Assert.True(consoleOutput.ToString().Contains(message));
        }
    }
}
