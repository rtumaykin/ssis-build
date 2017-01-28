using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SsisDeploy
{
    public class DeployArguments
    {
        public string DeploymentFilePath { get; private set; }

        public string ServerInstance { get; private set; }

        public string Catalog { get; private set; }

        public string Folder { get; private set; }

        public string ProjectName { get; private set; }

        public bool EraseSensitiveInfo { get; private set; }

        public string ProjectPassword { get; private set; }

        public static DeployArguments ProcessArgs(string[] args)
        {
            var deploymentArgs = new DeployArguments()
            {
                EraseSensitiveInfo = false
            };

            var startPos = 0;

            if (!args[0].StartsWith("-"))
            {
                deploymentArgs.DeploymentFilePath = args[0];
                startPos++;
            }

            for (var argPos = startPos; argPos < args.Length; argPos++)
            {
                switch (args[argPos].ToLowerInvariant())
                {
                    case "-serverinstance":
                        deploymentArgs.ServerInstance = args[argPos++ + 1];
                        break;

                    case "-catalog":
                        deploymentArgs.Catalog = args[argPos++ + 1];
                        break;

                    case "-folder":
                        deploymentArgs.Folder = args[argPos++ + 1];
                        break;

                    case "-projectname":
                        deploymentArgs.ProjectName = args[argPos++ + 1];
                        break;

                    case "-erasesensitiveinfo":
                        deploymentArgs.EraseSensitiveInfo = true;
                        break;

                    case "-projectpassword":
                        deploymentArgs.ProjectPassword = args[argPos++ + 1];
                        break;

                    default:
                        throw new InvalidArgumentException(args[argPos]);
                }
            }

            return deploymentArgs;
        }
    }
}
