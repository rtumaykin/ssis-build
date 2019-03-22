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
using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace SsisBuild.Core.Builder
{
    [Cmdlet(VerbsCommon.New, "SsisDeploymentPackage")]
    public class SsisBuildPowershell : PSCmdlet
    {
        [Parameter(HelpMessage = "Full path to a SSIS project file (with dtproj extension). " +
                                 "If a project file is not specified, ssisbuild searches current working directory for a " +
                                 "file with dtproj extension and uses that file.",
                Position = 0
            )]
        public string ProjectPath { get; set; }

        [Parameter(HelpMessage = "Full path to a folder where the ispac file will be created. " +
                                 "If ommitted, then the ispac file will be created in the bin/<Configuration> subfolder of the project folder.")]
        public string OutputFolder { get; set; }

        [Parameter(HelpMessage = "Overrides current project protection level. " +
                                 "Available values are DontSaveSensitive, EncryptAllWithPassword, EncryptSensitiveWithPassword")]
        public string ProtectionLevel { get; set; }

        [Parameter(HelpMessage = "Password to decrypt original project data if its current protection level is either EncryptAllWithPassword or " +
                                 "EncryptSensitiveWithPassword, in which case the value should be supplied, otherwise build will fail.")]
        public string Password { get; set; }

        [Parameter(HelpMessage = "Password to encrypt resulting deployment packageif its resulting protection level is either EncryptAllWithPassword " +
                                 "or EncryptSensitiveWithPassword. If ommitted, the value of the -Password switch is used for encryption, " +
                                 "unless original protection level was DontSaveSensitive. In this case the value should be supplied, otherwise build will fail.")]
        public string NewPassword { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Required. Name of project configuration to use.")]
        public string Configuration { get; set; }

        [Parameter(HelpMessage = "Path to a release notes file. Supports simple or complex release notes format, as defined here: " +
                                 "http://fsharp.github.io/FAKE/apidocs/fake-releasenoteshelper.html")]
        public string ReleaseNotes { get; set; }

        [Parameter(HelpMessage = "A collection of replacement project parameters.")]
        public Hashtable Parameters { get; set; }

        private IBuilder _builder;
        private string _workingFolder;

        internal void ProcessRecordInternal(IBuilder builder, string workingFolder)
        {
            _builder = builder;
            _workingFolder = workingFolder;
            ProcessRecord();
        }

        protected override void ProcessRecord()
        {
            _workingFolder = _workingFolder ?? CurrentProviderLocation("FileSystem").ProviderPath;

            var buildArguments = new BuildArguments(
                string.IsNullOrWhiteSpace(_workingFolder) ? null : _workingFolder, 
                string.IsNullOrWhiteSpace(ProjectPath) ? null : ProjectPath,
                string.IsNullOrWhiteSpace(OutputFolder) ? null : OutputFolder,
                string.IsNullOrWhiteSpace(ProtectionLevel) ? null : ProtectionLevel,
                string.IsNullOrWhiteSpace(Password) ? null : Password,
                string.IsNullOrWhiteSpace(NewPassword) ? null : NewPassword,
                string.IsNullOrWhiteSpace(Configuration) ? null : Configuration,
                string.IsNullOrWhiteSpace(ReleaseNotes) ? null : ReleaseNotes,
                Parameters?.OfType<DictionaryEntry>().ToDictionary(e => e.Key as string, e => e.Value as string)
            );

            _builder = _builder ?? new Builder();

            try
            {
                _builder.Build(buildArguments);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
