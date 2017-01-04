using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SsisBuild.Runner
{
    internal class Parameters
    {
        public string ProjectPath { get; set; }
        public bool TestOnly { get; set; }
        public IDictionary<string, string> BuildArguments { get; set; }

        public static Parameters ProcessArgs(string[] args)
        {

            var explicitProjectPath = false;
            var testOnly = false;
            var parameters = new Dictionary<string, string>();
            string projectPath;

            var argsList = args.ToList();

            // find the dtproj file name. Must be the first argument
            if (argsList.Count == 0 || argsList[0].Substring(0, 1) == "-")
            {
                // there is no explicit project path. Need to find it.
                projectPath = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.dtproj").FirstOrDefault();
            }
            else
            {
                projectPath = Path.IsPathRooted(args[0])
                    ? Path.GetFullPath(args[0])
                    : Path.Combine(Environment.CurrentDirectory, args[0]);

                argsList.RemoveAt(0);
            }

            // find if it is test parameters only
            if (argsList.Count != 0 && argsList[0].ToLowerInvariant() == "-t")
            {
                testOnly = true;
                argsList.RemoveAt(0);
            }

            if (!testOnly && !File.Exists(projectPath))
            {
                throw new ArgumentProcessingException($"Project file \"{projectPath}\" does not exist.");
            }

            while (argsList.Count > 0)
            {
                if (argsList[0].Substring(0, 3).ToLowerInvariant() != "-p:")
                {
                    throw new ArgumentProcessingException($"Invalid argument \"{argsList[0]}\". Expected argument starting with \"-p:\".");
                }
                if (argsList.Count == 1)
                {
                    throw new ArgumentProcessingException($"No value provided for parameter \"{argsList[0]}\".");
                }


                var parameterName = argsList[0].Length < 4 ? "" : argsList[0].Substring(3);

                if (!Regex.IsMatch(parameterName, "[_a-zA-Z][_a-zA-Z0-9]"))
                {
                    throw new ArgumentProcessingException($"Invalid identifier passed as parameter \"{argsList[0]}\".");
                }

                parameters.Add(parameterName, argsList[1]);

                argsList.RemoveRange(0, 2);
            }

            return new Parameters()
            {
                ProjectPath = projectPath,
                TestOnly = testOnly,
                BuildArguments = parameters
            };

        }
    }
}
