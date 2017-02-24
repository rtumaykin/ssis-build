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

namespace SsisBuild.Core.ProjectManagement
{
    public abstract class Parameter : IParameter
    {
        public string Name { get; protected set; }
        public string Value { get; protected set; }
        public ParameterSource Source { get; private set; }
        public bool Sensitive { get; protected set; }

        public Type ParameterDataType { get; protected set; }

        protected XmlElement ValueElement;
        protected XmlElement ParentElement;

        protected XmlNode ParameterNode;
        protected string ScopeName;

        protected Parameter(string scopeName, XmlNode parameterNode, ParameterSource source)
        {
            if (parameterNode == null)
                throw new ArgumentNullException(nameof(parameterNode));

            Source = source;
            ParameterNode = parameterNode;
            ScopeName = scopeName;
        }

        protected abstract void InitializeFromXml();

        public void SetValue(string value, ParameterSource source)
        {
            Value = value;
            Source = source;

            if (value == null && ValueElement.ParentNode != null)
            {
                ParentElement.RemoveChild(ValueElement);
            }

            if (value != null && ValueElement.ParentNode == null)
            {
                ParentElement.AppendChild(ValueElement);
            }
            if (value != null)
                ValueElement.InnerText = value;
            else
            {
                foreach (XmlNode childNode in ValueElement.ChildNodes)
                {
                    if (childNode.NodeType != XmlNodeType.Attribute)
                        ValueElement.RemoveChild(childNode);
                }
            }
        }
    }
}
