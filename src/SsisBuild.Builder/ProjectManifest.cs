using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime;

namespace SsisBuild
{
    public class ProjectManifest
    {
        public DTSProtectionLevel ProtectionLevel { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public List<PackageManifest> Packages { get; set; }
        public List<string> ConnectionManagers { get; set; }
    }
}
