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
using System.Xml;
using SsisBuild.Core.Helpers;

namespace SsisBuild.Core
{
    public sealed class ConfigurationParameter : Parameter
    {
        public ConfigurationParameter(XmlNode parameterNode, bool sensitive) : base(null, parameterNode, ParameterSource.Configuration)
        {
            Sensitive = sensitive;
            InitializeFromXml();
        }

        protected override void InitializeFromXml()
        {
            var namespaceManager = ParameterNode.GetNameSpaceManager();

            var id = ParameterNode.SelectSingleNode("./Id", namespaceManager);
            if (id != null)
            {
                Guid testId;
                if (Guid.TryParse(id.InnerText, out testId))
                {
                    var name = ParameterNode.SelectSingleNode("./Name", namespaceManager)?.InnerText;
                    if (name == null)
                        throw new InvalidXmlException("Name element is missing", ParameterNode);

                    var valueNode = ParameterNode.SelectSingleNode("./Value", namespaceManager);
                    var value = valueNode?.InnerText;

                    // Don't store encrypted string
                    if (Sensitive)
                        value = null;

                    Name = name;
                    Value = value;
                }
            }
            else
            {
                throw new InvalidXmlException("Id element is missing", ParameterNode);
            }
        }
    }
}
