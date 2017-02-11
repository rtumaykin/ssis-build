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

namespace SsisBuild
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                MainInternal(args, new Builder(), new BuildArguments());
            }
            catch (ArgumentsProcessingException x)
            {
                Console.WriteLine(x.Message);
                Usage();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }
        }

        internal static void MainInternal(string[] args, IBuilder builder, IBuildArguments buildArguments)
        {
            buildArguments.ProcessArgs(args);
            builder.Build(buildArguments);
        }

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
