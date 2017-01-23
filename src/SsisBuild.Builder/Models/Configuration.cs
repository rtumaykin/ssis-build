using System;
using System.Collections.Generic;
using System.Xml;

namespace SsisBuild.Models
{
    public class Configuration : ProjectFile
    {
        private readonly string _configurationName;
        public Configuration(string configurationName)
        {
            _configurationName = configurationName;
        }

        protected override IList<Parameter> ExtractParameters()
        {
            var parameters = new List<Parameter>();

            var parameterNodes =
                 FileXmlDocument.SelectNodes(
                     $"/Project/Configurations/Configuration[Name=\"{_configurationName}\"]/Options/ParameterConfigurationValues/ConfigurationSetting", NamespaceManager);

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
                        var valueNode = parameterNode.SelectSingleNode("./Value", NamespaceManager);
                        var value = valueNode?.InnerText;
                        if (name != null)
                            parameters.Add(new Parameter(name, value, ParameterSource.Configuration, valueNode as XmlElement, parameterNode as XmlElement));
                    }
                }
            }

            return parameters;
        }
    }
}
