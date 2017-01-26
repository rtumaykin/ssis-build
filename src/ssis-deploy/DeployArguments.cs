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
            var argsList = args.ToList();
            var deploymentArgs = new DeployArguments()
            {
                EraseSensitiveInfo = false
                
            };

            var startPos = 0;

            if (!argsList[0].StartsWith("-"))
            {
                deploymentArgs.DeploymentFilePath = argsList[0];
                startPos++;
            }

            for (var argPos = startPos; argPos < argsList.Count; argPos++)
            {
                switch (argsList[argPos].ToLowerInvariant())
                {
                    case "-serverinstance":
                        deploymentArgs.ServerInstance = argsList[argPos++ + 1];
                        break;

                    case "-catalog":
                        deploymentArgs.Catalog = argsList[argPos++ + 1];
                        break;

                    case "-folder":
                        deploymentArgs.Folder = argsList[argPos++ + 1];
                        break;

                    case "-projectname":
                        deploymentArgs.ProjectName = argsList[argPos++ + 1];
                        break;

                    case "-erasesensitiveinfo":
                        deploymentArgs.EraseSensitiveInfo = true;
                        break;

                    case "-projectpassword":
                        deploymentArgs.ProjectPassword = argsList[argPos++ + 1];
                        break;

                    default:
                        throw new InvalidArgumentException(argsList[argPos]);
                }
            }

            return deploymentArgs;
        }
    }
}
