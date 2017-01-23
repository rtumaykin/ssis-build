using System;
using System.Collections.Generic;
using System.Xml;

namespace SsisBuild.Models
{
    public class UserConfiguration : ProjectFile
    {
        private readonly string _configurationName;

        public UserConfiguration(string configurationName)
        {
            _configurationName = configurationName;
        }

        protected override void DecryptNode(XmlNode node, string password)
        {
            // Do nothing since it is encrypted by a user key.
        }

        protected override IList<Parameter> ExtractParameters()
        {
            var parameters = new List<Parameter>();
            var parameterNodes =
                 FileXmlDocument.SelectNodes(
                     $"/DataTransformationsUserConfiguration/Configurations/Configuration[Name=\"{_configurationName}\"]/Options/ParameterConfigurationSensitiveValues/ConfigurationSetting", NamespaceManager);

            if (parameterNodes == null)
                return parameters;

            foreach (XmlNode parameterNode in parameterNodes)
            {
                var id = parameterNode.SelectSingleNode("./Id", NamespaceManager);
                if (id != null)
                {
                    Guid testId;
                    if (Guid.TryParse(id.InnerText, out testId))
                    {
                        var name = parameterNode.SelectSingleNode("./Name", NamespaceManager)?.InnerText;
                        if (name != null)
                            parameters.Add(new Parameter(name, null, ParameterSource.Configuration, null, parameterNode as XmlElement));
                    }
                }
            }

            return parameters;
        }
    }
}
