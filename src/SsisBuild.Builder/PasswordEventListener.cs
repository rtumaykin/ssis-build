using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime;

namespace SsisBuild
{
    public class PasswordEventListener : DefaultEvents
    {
        public bool NeedPassword;

        public override bool OnError(DtsObject source, int errorCode, string subComponent, string description, string helpFile, int helpContext, string idofInterfaceWithError)
        {
            if (errorCode == -1073659809)
                this.NeedPassword = true;
            return false;
        }
    }
}
