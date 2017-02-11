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
using Xunit;

namespace SsisBuild.Logger.Tests
{
    public class LoggerTests
    {
        [Fact]
        public void Pass_Message()
        {
            var stdOut = Console.Out;
            try
            {

                var consoleOutput = new StringWriter();
                Console.SetOut(consoleOutput);
                var logger = new ConsoleLogger();
                logger.LogMessage("Message");
                Assert.True(consoleOutput.ToString().Contains("Message"));
            }
            finally
            {
                Console.SetOut(stdOut);
            }
        }

        [Fact]
        public void Pass_Warning()
        {
            var stdOut = Console.Out;
            try
            {

                var consoleOutput = new StringWriter();
                Console.SetOut(consoleOutput);
                var logger = new ConsoleLogger();
                logger.LogWarning("Warning");
                Assert.True(consoleOutput.ToString().Contains("Warning"));
            }
            finally
            {
                Console.SetOut(stdOut);
            }
        }

        [Fact]
        public void Pass_Error()
        {
            var stdOut = Console.Out;
            try
            {

                var consoleOutput = new StringWriter();
                Console.SetOut(consoleOutput);
                var logger = new ConsoleLogger();
                logger.LogError("Error");
                Assert.True(consoleOutput.ToString().Contains("Error"));
            }
            finally
            {
                Console.SetOut(stdOut);
            }
        }
    }
}
