using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace SsisBuild.Core.Builder
{
    [Cmdlet(VerbsCommon.New, "SsisDeploymentPackage")]
    public class SsisBuildPowershell : PSCmdlet
    {
        [Parameter(
            Position = 0,
            HelpMessage =
                "Full path to a SSIS project file (with dtproj extension). If a project file is not specified, ssisbuild searches current working directory, for a file with dtproj extension and uses that file."
        )]
        public string ProjectPath { get; set; }

        [Parameter]
        public string OutputFolder { get; set; }

        [Parameter]
        public string ProtectionLevel { get; set; }

        [Parameter]
        public string Password { get; set; }

        [Parameter]
        public string NewPassword { get; set; }

        [Parameter(Mandatory = true)]
        public string Configuration { get; set; }

        [Parameter]
        public string ReleaseNotes { get; set; }

        [Parameter]
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
                _workingFolder, 
                ProjectPath, 
                OutputFolder, 
                ProtectionLevel, 
                Password, 
                NewPassword, 
                Configuration, 
                ReleaseNotes,
                Parameters.OfType<DictionaryEntry>().ToDictionary(e => e.Key as string, e => e.Value as string)
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
