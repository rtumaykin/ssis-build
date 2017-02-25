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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SsisBuild.Core.Deployer;

namespace SsisDeploy
{
    class Program
    {
        private static readonly string[] ParameterNames = {
            nameof(DeployArguments.Catalog),
            nameof(DeployArguments.DeploymentFilePath),
            nameof(DeployArguments.EraseSensitiveInfo),
            nameof(DeployArguments.Folder),
            nameof(DeployArguments.ProjectName),
            nameof(DeployArguments.ProjectPassword),
            nameof(DeployArguments.ServerInstance),
        };


        [ExcludeFromCodeCoverage]
        static void Main(string[] args)
        {
            try
            {
                MainInternal(new Deployer(), args);
            }
            catch
            {
                Environment.Exit(1);
            }
        }

        internal static void MainInternal(IDeployer deployer, string[] args)
        {
            try
            {
                var deployArguments = ParseCommandLineArguments(args);
                deployer.Deploy(deployArguments);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
                if (e is CommandLineParsingException || e is DeployArgumentsValidationException)
                    Usage();
                throw;
            }
        }

        internal static DeployArguments ParseCommandLineArguments(string[] args)
        {
            var startPos = 0;

            string deploymentFilePath = null;
            string serverInstance = null;
            string catalog = null;
            string folder = null;
            string projectName = null;
            string projectPassword = null;
            var eraseSensitiveInfo = false;

            if (args.Length > 0 && !args[0].StartsWith("-"))
            {
                deploymentFilePath = args[0];

                startPos++;
            }

            for (var argPos = startPos; argPos < args.Length; argPos++)
            {
                var argName = args[argPos];
                var argValue = argPos == args.Length - 1 ? null : args[argPos + 1];

                if (!argName.StartsWith("-"))
                    throw new InvalidTokenException(argName);

                var lookupArgName = ParameterNames.FirstOrDefault(n => n.Equals(argName.Substring(1), StringComparison.InvariantCultureIgnoreCase)) ?? argName.Substring(1);

                switch (lookupArgName)
                {
                    case nameof(DeployArguments.ServerInstance):
                        serverInstance = args[argPos++ + 1];
                        break;

                    case nameof(DeployArguments.Catalog):
                        catalog = args[argPos++ + 1];
                        break;

                    case nameof(DeployArguments.Folder):
                        folder = args[argPos++ + 1];
                        break;

                    case nameof(DeployArguments.ProjectName):
                        projectName = args[argPos++ + 1];
                        break;

                    case nameof(DeployArguments.EraseSensitiveInfo):
                        eraseSensitiveInfo = true;
                        break;

                    case nameof(DeployArguments.ProjectPassword):
                        projectPassword = args[argPos++ + 1];
                        break;

                    default:
                        throw new InvalidTokenException(argName);
                }
            }

            return new DeployArguments(Environment.CurrentDirectory, deploymentFilePath, serverInstance, catalog, folder, projectName, projectPassword, eraseSensitiveInfo);
        }


        private static void Usage()
        {
            var usage = new[]
            {
                "---------------------------------------------------------------",
                "Usage:",
                "",
                "Syntax:                ssisdeploy [Ispac File] [-<Switch Name> <Value>] [...[-<Switch Name> <Value>]] [-EraseSensitiveInfo]",
                "",
                "Description:           Deploys an Ispac file to an SSIS Catalog.",
                "",
                "Switches:",
                "",
                "  Ispac File:          Full path to an SSIS deployment file (with ispac extension). If a deployment file is not specified, ssisdeploy searches current working directory",
                "                       for a file with ispac extension and uses that file.",
                "",
                "  -ServerInstance:     Required. Full Name of the target SQL Server instance.",
                "",
                "  -Catalog:            Required. Name of the SSIS Catalog on the target server.",
                "",
                "  -Folder:             Required. Deployment folder within destination catalog.",
                "",
                "  -ProjectName:        Required. Name of the project in the destination folder.",
                "",
                "  -ProjectPassword:    Password to decrypt sensitive data for deployment.",
                "",
                "  -EraseSensitiveInfo: Option to remove all sensitive info from the deployment ispac and deploy all sensitive parameters separately. If not specified then sensitive data will not be removed.",
                "",
                "Example:",
                "     ssisdeploy sample.ispac -ServerInstance dbserver\\instance -Catalog SSISDB -Folder SampleFolder -ProjectName Sample -ProjectPassword xyz -EraseSensitiveInfo"
            };

            foreach (var usageLine in usage)
            {
                Console.WriteLine(usageLine);
            }
        }
    }
}
