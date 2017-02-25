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
using System.Linq;
using SsisBuild.Core.Builder;

namespace SsisBuild
{
    class Program
    {
        private static readonly string[] ParameterNames = {
            nameof(BuildArguments.OutputFolder),
            nameof(BuildArguments.Configuration),
            nameof(BuildArguments.ProtectionLevel),
            nameof(BuildArguments.Password),
            nameof(BuildArguments.ReleaseNotes),
            nameof(BuildArguments.NewPassword)
        };

        static void Main(string[] args)
        {
            try
            {
                MainInternal(new Builder(), args);
            }
            catch
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Internal method that allows injecting custom objects during the Unit testing sessions
        /// </summary>
        /// <param name="builder">Implementation of <see cref="IBuilder"/> interface.</param>
        /// <param name="args">Command line arguments.</param>
        internal static void MainInternal(IBuilder builder, string[] args)
        {
            try
            {
                var buildArguments = ParseCommandLineArguments(args);
                builder.Build(buildArguments);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
                if (e is CommandLineParsingException || e is BuildArgumentsValidationException)
                    Usage();
                throw;
            }
        }

        /// <summary>
        /// Parses Command Line arguments
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>an instance of <see cref="BuildArguments"/> class</returns>
        private static BuildArguments ParseCommandLineArguments(string[] args)
        {
            var startPos = 0;

            string projectPath = null;
            string configuration = null;
            string outputFolder = null;
            string protectionLevel = null;
            string password = null;
            string newPassword = null;
            string releaseNotes = null;
            var parameters = new Dictionary<string, string>();


            if (args.Length > 0 && !args[0].StartsWith("-"))
            {
                projectPath = args[0];
                startPos++;
            }

            for (var argPos = startPos; argPos < args.Length; argPos += 2)
            {
                var argName = args[argPos];
                var argValue = argPos == args.Length - 1 ? null : args[argPos + 1];

                if (!argName.StartsWith("-"))
                    throw new InvalidTokenException(argName);

                var lookupArgName = ParameterNames.FirstOrDefault(n => n.Equals(argName.Substring(1), StringComparison.InvariantCultureIgnoreCase)) ?? argName.Substring(1);

                switch (lookupArgName)
                {
                    case nameof(BuildArguments.Configuration):
                        configuration = argValue;
                        break;

                    case nameof(BuildArguments.OutputFolder):
                        outputFolder = argValue;
                        break;

                    case nameof(BuildArguments.ProtectionLevel):
                        protectionLevel = argValue;
                        break;

                    case nameof(BuildArguments.Password):
                        password = argValue;
                        break;

                    case nameof(BuildArguments.NewPassword):
                        newPassword = argValue;
                        break;

                    case nameof(BuildArguments.ReleaseNotes):
                        releaseNotes = argValue;
                        break;

                    default:
                        if (argName.StartsWith("-Parameter:", StringComparison.InvariantCultureIgnoreCase))
                        {
                            parameters.Add(argName.Substring(11), argValue);
                        }
                        else
                        {
                            throw new InvalidTokenException(argName);
                        }
                        break;
                }

                if (argValue == null)
                    throw new NoValueProvidedException(lookupArgName);
            }

            return new BuildArguments(Environment.CurrentDirectory, projectPath, outputFolder, protectionLevel, password, newPassword, configuration, releaseNotes, parameters); 
        }

        /// <summary>
        /// Displays the ssisbuild.exe usage.
        /// </summary>
        private static void Usage()
        {
            var usage = new[]
            {
                "---------------------------------------------------------------",
                "Usage:",
                "",
                "Syntax:                ssisbuild [Project File] [-<Switch Name> <Value>] [...[-<Switch Name> <Value>]] [-Parameter:<Name> <Value>] [...[-Parameter:<Name> <Value>]]",
                "",
                "Description:           Builds an SSIS project into an ispac file. Project must not be encrypted by a user key.",
                "",
                "Switches:",
                "",
                "  Project File:        Full path to a SSIS project file (with dtproj extension). If a project file is not specified, ssisbuild searches current working directory",
                "                       for a file with dtproj extension and uses that file.",
                "",
                "  -Configuration:      Required. Name of project configuration to use.",
                "",
                "  -OutputFolder:       Full path to a folder where the ispac file will be created. If ommitted, then the ispac file will be created in the",
                "                       bin/<Configuration> subfolder of the project folder.",
                "",
                "  -ProtectionLevel:    Overrides current project protection level. Available values are DontSaveSensitive, EncryptAllWithPassword, EncryptSensitiveWithPassword.",
                "",
                "  -Password:           Password to decrypt original project data if its current protection level is either EncryptAllWithPassword or EncryptSensitiveWithPassword, ",
                "                       in which case the value should be supplied, otherwise build will fail.",
                "",
                "  -NewPassword:        Password to encrypt resulting project if its resulting protection level is either EncryptAllWithPassword or EncryptSensitiveWithPassword.",
                "                       If ommitted, the value of the <Password> switch is used for encryption, unless original protection level was DontSaveSensitive,",
                "                       in which case the value should be supplied, otherwise build will fail.",
                "",
                "  -Parameter:          Project or Package parameter. Name is a standard full parameter name including the scope. For example Project::Parameter1. During the build,",
                "                       these values will replace existing values regardless of what these values were originally.",
                "",
                "  -ReleaseNotes:       Path to a release notes file. File can have simple or complex release notes format.",
                "",
                "Example:",
                "     ssisbuild example.dtproj -Configuration Release -Parameter:SampleParameter \"some value\""
            };

            foreach (var usageLine in usage)
            {
                Console.WriteLine(usageLine);
            }
        }
    }
}
