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
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using SsisBuild.Core;

namespace SsisDeploy
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MainInternal(new Deployer(), new DeployArguments(), args);
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

        internal static void MainInternal(IDeployer deployer, IDeployArguments deployArguments, string[] args)
        {
            deployArguments.ProcessArgs(args);
            deployer.Deploy(deployArguments);
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
                "  Ispac File:          Full path to an SSIS deployment file (with ispac extension). If a project file is not specified, ssisdeploy searches current working directory",
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
