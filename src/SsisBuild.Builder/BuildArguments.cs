using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public IDictionary<string, string> SensitiveParameters { get; set; }

    }
}
