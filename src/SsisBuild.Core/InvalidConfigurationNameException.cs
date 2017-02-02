using System;

namespace SsisBuild.Core
{
    public class InvalidConfigurationNameException : Exception
    {
        public string ConfigurationName { get; }

        public InvalidConfigurationNameException(string configurationName) : base($"Invalid configuration name: {configurationName}")
        {
            ConfigurationName = configurationName;
        }
        
    }
}