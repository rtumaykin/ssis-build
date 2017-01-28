using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SsisBuild.Core
{
    public class ProjectNotLoadedException : Exception
    {
        public ProjectNotLoadedException() : base("Project must be loaded from dtproj or ispac file before this operation can be performed")
        {
        }
    }
}
