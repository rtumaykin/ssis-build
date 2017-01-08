using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime;

namespace SsisBuild
{
    internal class ParameterDetail
    {
        public string FullName { get; set; }
        public Guid Id { get; set; }
        public bool IsSensitive { get; set; }
        public object OriginalValue { get; set; }
        public bool IsInConfiguration { get; set; }
        public string ConfigurationName { get; set; }
        public object ConfigurationValue { get; set; }
        public bool IsInBuldParameters { get; set; }
        public string BuildParameterValue { get; set; }
        public bool ForceSensitive { get; set; }
        public Parameter Parameter { get; set; }
    }
}
