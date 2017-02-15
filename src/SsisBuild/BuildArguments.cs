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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace SsisBuild 
{
    public sealed class BuildArguments : IBuildArguments
    {
        private static readonly string[] ParameterNames = {
            nameof(OutputFolder),
            nameof(Configuration),
            nameof(ProtectionLevel),
            nameof(Password),
            nameof(ReleaseNotes),
            nameof(NewPassword)
        };

        public string ProjectPath { get; private set; }
        public string OutputFolder { get; private set; }
        public string ProtectionLevel { get; private set; }
        public string Password { get; private set; }
        public string NewPassword { get; private set; }
        public string Configuration { get; private set; }
        public string ReleaseNotes { get; private set; }
        public IReadOnlyDictionary<string, string> Parameters { get; }

        private readonly IDictionary<string, string> _parameters;

        public BuildArguments()
        {
            _parameters = new Dictionary<string, string>();
            Parameters = new ReadOnlyDictionary<string, string>(_parameters);
        }

        private void Parse(string[] args)
        {
            var startPos = 0;
            if (args.Length > 0 && !args[0].StartsWith("-"))
            {
                ProjectPath = Path.IsPathRooted(args[0])
                    ? Path.GetFullPath(args[0])
                    : Path.Combine(Environment.CurrentDirectory, args[0]);

                startPos++;
            }
            else
            {
                ProjectPath = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.dtproj").FirstOrDefault();
            }

            for (var argPos = startPos; argPos < args.Length; argPos += 2)
            {
                var argName = args[argPos];
                var argValue = argPos == args.Length - 1 ? null : args[argPos + 1];

                if (!argName.StartsWith("-"))
                    throw new InvalidTokenException(argName);

                var lookupArgName = ParameterNames.FirstOrDefault(n => n.Equals(argName.Substring(1), StringComparison.InvariantCultureIgnoreCase)) ?? argName.Substring(1);
                // var lookupArgName = (_parameterNames.FirstOrDefault(n => n.Substring(1).Equals(argName, StringComparison.InvariantCultureIgnoreCase)) ?? argName).Substring(1);

                switch (lookupArgName)
                {
                    case nameof(Configuration):
                        Configuration = argValue;
                        break;

                    case nameof(OutputFolder):
                        OutputFolder = argValue;
                        break;

                    case nameof(ProtectionLevel):
                        ProtectionLevel = argValue;
                        break;

                    case nameof(Password):
                        Password = argValue;
                        break;

                    case nameof(NewPassword):
                        NewPassword = argValue;
                        break;

                    case nameof(ReleaseNotes):
                        ReleaseNotes = argValue;
                        break;

                    default:
                        if (argName.StartsWith("-Parameter:", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _parameters.Add(argName.Substring(11), argValue);
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
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(ProjectPath))
                throw new ProjectFileNotFoundException(Environment.CurrentDirectory);

            if (ProtectionLevel != null && !(new[] { "DontSaveSensitive", "EncryptAllWithPassword", "EncryptSensitiveWithPassword" }.Contains(ProtectionLevel ?? string.Empty, StringComparer.InvariantCultureIgnoreCase)))
                throw new InvalidArgumentException(nameof(ProtectionLevel), ProtectionLevel);

            if (Configuration == null)
                throw new MissingRequiredArgumentException(nameof(Configuration));

            if (new[] { "EncryptAllWithPassword", "EncryptSensitiveWithPassword" }.Contains(ProtectionLevel) && string.IsNullOrWhiteSpace(NewPassword ?? Password))
                throw new PasswordRequiredException(ProtectionLevel);

            if (ProtectionLevel == "DontSaveSensitive" && !string.IsNullOrWhiteSpace(NewPassword))
                throw new DontSaveSensitiveWithPasswordException();

        }

        public void ProcessArgs(string[] args)
        {
            Parse(args);

            Validate();
        }
    }
}
