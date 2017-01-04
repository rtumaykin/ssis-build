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
    internal class Switches
    {
        public string ProjectPath { get; set; }
        public string OutputFolder { get; set; }
        public string ProtectionLevel { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
        public string Configuration { get; set; }
        public IDictionary<string, string> Parameters { get; set; }

        public static Switches ProcessArgs(string[] args)
        {
            var switches = new Switches()
            {
                Parameters = new Dictionary<string, string>()
            };

            var explicitProjectPath = false;
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

            if (!File.Exists(projectPath))
            {
                throw new ArgumentProcessingException($"Project file \"{projectPath}\" does not exist.");
            }

            switches.ProjectPath = projectPath;

            while (argsList.Count > 0)
            {
                if (argsList.Count == 1)
                {
                    throw new ArgumentProcessingException($"No value provided for parameter \"{argsList[0]}\".");
                }

                switch (argsList[0])
                {
                    case "-Configuration":
                        switches.Configuration = argsList[1];
                        break;

                    case "-OutputFolder":
                        switches.OutputFolder = argsList[1];
                        break;

                    case "-ProtectionLevel":
                        if (
                            !(new[] {"DontSaveSensitive", "EncryptAllWithPassword", "EncryptSensitiveWithPassword"}
                                .Contains(argsList[1])))
                        {
                            throw new ArgumentProcessingException($"Unknown Protection Level: \"{argsList[1]}\"");
                        }
                        switches.ProtectionLevel = argsList[1];
                        break;

                    case "-Password":
                        switches.Password = argsList[1];
                        break;

                    case "-NewPassword":
                        switches.NewPassword = argsList[1];
                        break;

                    default:
                        if (argsList[0].Substring(0, 11) == "-Parameter:")
                        {
                            switches.Parameters.Add(argsList[0].Substring(11), argsList[1]);
                        }
                        else
                        {
                            throw new ArgumentProcessingException($"Unknown switch \"{argsList[0]}\"");
                        }
                        break;
                }
                argsList.RemoveRange(0, 2);
            }

            if (string.IsNullOrWhiteSpace(switches.Configuration))
                throw new ArgumentProcessingException("Configuration name must be specified");

            return switches;
        }
    }
}
