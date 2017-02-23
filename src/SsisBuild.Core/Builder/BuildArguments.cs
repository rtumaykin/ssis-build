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

namespace SsisBuild.Core.Builder 
{
    public sealed class BuildArguments : IBuildArguments
    {
        public string WorkingFolder { get; }
        public string ProjectPath { get; }
        public string OutputFolder { get; }
        public string ProtectionLevel { get; }
        public string Password { get; }
        public string NewPassword { get; }
        public string Configuration { get; }
        public string ReleaseNotes { get; }
        public IDictionary<string, string> Parameters { get; }

        public BuildArguments(
            string workingFolder, 
            string projectPath, 
            string outputFolder, 
            string protectionLevel, 
            string password,
            string newPassword, 
            string configuration, 
            string releaseNotes, 
            IDictionary<string, string> parameters
        )
        {
            WorkingFolder = workingFolder;
            ProjectPath = projectPath;
            OutputFolder = outputFolder;
            ProtectionLevel = protectionLevel;
            Password = password;
            NewPassword = newPassword;
            Configuration = configuration;
            ReleaseNotes = releaseNotes;
            Parameters = parameters ?? new Dictionary<string, string>();

            Validate();
        }

        private void Validate()
        {
            if (ProtectionLevel != null && !(new[]
            {
                nameof(ProjectManagement.ProtectionLevel.DontSaveSensitive),
                nameof(ProjectManagement.ProtectionLevel.EncryptAllWithPassword),
                nameof(ProjectManagement.ProtectionLevel.EncryptSensitiveWithPassword)
            }.Contains(ProtectionLevel ?? string.Empty, StringComparer.InvariantCultureIgnoreCase)))
                throw new InvalidArgumentException(nameof(ProtectionLevel), ProtectionLevel);

            if (Configuration == null)
                throw new MissingRequiredArgumentException(nameof(Configuration));

            if (new[] {
                nameof(ProjectManagement.ProtectionLevel.EncryptAllWithPassword),
                nameof(ProjectManagement.ProtectionLevel.EncryptSensitiveWithPassword)
            }.Contains(ProtectionLevel) && string.IsNullOrWhiteSpace(NewPassword ?? Password))
                throw new PasswordRequiredException(ProtectionLevel);

            if (ProtectionLevel == nameof(ProjectManagement.ProtectionLevel.DontSaveSensitive) && !string.IsNullOrWhiteSpace(NewPassword))
                throw new DontSaveSensitiveWithPasswordException();

        }
    }
}
