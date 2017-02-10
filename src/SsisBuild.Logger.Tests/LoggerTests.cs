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
