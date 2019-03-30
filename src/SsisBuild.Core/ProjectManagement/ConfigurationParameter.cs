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
using SsisBuild.Core.ProjectManagement.Helpers;

namespace SsisBuild.Core.ProjectManagement
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
                    ParameterDataType = ExtractDataType();
                    Value = value;
                }
            }
            else
            {
                throw new InvalidXmlException("Id element is missing", ParameterNode);
            }
        }

        private Type ExtractDataType()
        {
            var dataType = ParameterNode.SelectSingleNode("./Value", ParameterNode.GetNameSpaceManager())?.Attributes?["xsi:type"]?.Value;

            switch (dataType)
            {
                case "xsd:boolean":
                    return typeof(bool);

                case "xsd:unsignedByte":
                    return typeof(byte);

                case "xsd:dateTime":
                    return typeof(DateTime);

                case "xsd:decimal":
                    return typeof(decimal);

                case "xsd:double":
                    return typeof(double);

                case "xsd:short":
                    return typeof(short);

                case "xsd:int":
                    return typeof(int);

                case "xsd:long":
                    return typeof(long);

                case "xsd:byte":
                    return typeof(sbyte);

                case "xsd:float":
                    return typeof(float);

                case "xsd:string":
                    return typeof(string);

                case "xsd:unsignedInt":
                    return typeof(uint);

                case "xsd:unsignedLong":
                    return typeof(ulong);

                default:
                    return null;
            }
        }
    }
}
