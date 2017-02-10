using System;

namespace SsisBuild.Core
{
    public class InvalidProtectionLevelException : Exception
    {
        public ProtectionLevel ProtectionLevel { get; }

        public InvalidProtectionLevelException(ProtectionLevel protectionLevel) : base($"Invalid Protection Level for Deployment Package: {protectionLevel}.")
        {
            ProtectionLevel = protectionLevel;
        }
    }
}