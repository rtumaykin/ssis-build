using System;

namespace SsisBuild.Core
{
    public class InvalidProtectionLevelException : Exception
    {
        public InvalidProtectionLevelException (ProtectionLevel protectionLevel) : base($"Invalid Protection Level for Deployment Package: {protectionLevel}.") { }
    }
}