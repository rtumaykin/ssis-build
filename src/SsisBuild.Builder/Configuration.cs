//-----------------------------------------------------------------------
//   Copyright 2017 Roman Tumaykin
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;

namespace SsisBuild.Core
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
