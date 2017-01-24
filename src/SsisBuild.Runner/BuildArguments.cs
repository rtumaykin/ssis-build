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
using System.IO;
using System.Linq;

namespace SsisBuild
{
    public class BuildArguments
    {
        public string ProjectPath { get; set; }
        public string OutputFolder { get; set; }
        public string ProtectionLevel { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
        public string ConfigurationName { get; set; }
        public string ReleaseNotesFilePath { get; set; }
        public IDictionary<string, string> Parameters { get; set; }

        public static BuildArguments ProcessArgs(string[] args)
        {
            var buildArguments = new BuildArguments()
            {
                Parameters = new Dictionary<string, string>()
            };

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

            buildArguments.ProjectPath = projectPath;

            while (argsList.Count > 0)
            {
                if (argsList.Count == 1)
                {
                    throw new ArgumentProcessingException($"No value provided for parameter \"{argsList[0]}\".");
                }

                switch (argsList[0])
                {
                    case "-Configuration":
                        buildArguments.ConfigurationName = argsList[1];
                        break;

                    case "-OutputFolder":
                        buildArguments.OutputFolder = argsList[1];
                        break;

                    case "-ProtectionLevel":
                        if (
                            !(new[] { "DontSaveSensitive", "EncryptAllWithPassword", "EncryptSensitiveWithPassword" }
                                .Contains(argsList[1], StringComparer.InvariantCultureIgnoreCase)))
                        {
                            throw new ArgumentProcessingException($"Unknown Protection Level: \"{argsList[1]}\"");
                        }
                        buildArguments.ProtectionLevel = argsList[1];
                        break;

                    case "-Password":
                        buildArguments.Password = argsList[1];
                        break;

                    case "-NewPassword":
                        buildArguments.NewPassword = argsList[1];
                        break;

                    case "-ReleaseNotes":
                        buildArguments.ReleaseNotesFilePath = argsList[1];
                        break;

                    default:
                        if (argsList[0].StartsWith("-Parameter:"))
                        {
                            buildArguments.Parameters.Add(argsList[0].Substring(11), argsList[1]);
                        }
                        else
                        {
                            throw new ArgumentProcessingException($"Unknown switch \"{argsList[0]}\"");
                        }
                        break;
                }
                argsList.RemoveRange(0, 2);
            }

            if (string.IsNullOrWhiteSpace(buildArguments.ConfigurationName))
                throw new ArgumentProcessingException("Configuration name must be specified");

            return buildArguments;
        }
    }
}
