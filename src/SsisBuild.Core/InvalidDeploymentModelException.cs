using System;

namespace SsisBuild.Core
{
    public class InvalidDeploymentModelException : Exception
    {
        public InvalidDeploymentModelException () : base ("This build method only apply to Project deployment model.") { }
    }
}