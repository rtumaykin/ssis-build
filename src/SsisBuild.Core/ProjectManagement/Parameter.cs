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
using System.ComponentModel;
using System.Globalization;
using System.Xml;

namespace SsisBuild.Core.ProjectManagement
{
    public abstract class Parameter : IParameter
    {
        public string Name { get; protected set; }

        public string Value
        {
            get { return _value; }
            protected set
            {
                if (ParameterDataType == null)
                    throw new InvalidCastException("Parameter Data Type is not set.");

                if (ParameterDataType != typeof(string) && !string.IsNullOrEmpty(value))
                {
                    if (ParameterDataType == typeof(DateTime))
                    {
                        try
                        {
                            _value = DateTime.Parse(value).ToString("s");
                        }
                        catch (Exception e)
                        {
                            throw new NotSupportedException($"Conversion to datetime failed for value {value}", e);
                        }
                    }
                    else if (ParameterDataType == typeof(bool))
                    {
                        try
                        {
                            _value = Boolean.Parse(value).ToString().ToLowerInvariant();
                        }
                        catch (Exception e)
                        {
                            throw new NotSupportedException($"Conversion to boolean failed for value {value}", e);
                        }
                    }
                    else
                    {
                        _value = TypeDescriptor.GetConverter(ParameterDataType).ConvertFromInvariantString(value)
                            ?.ToString();
                    }
                }
                else
                {
                    _value = value;
                }
            }
        }

        public ParameterSource Source { get; private set; }
        public bool Sensitive { get; protected set; }

        public Type ParameterDataType { get; protected set; }

        protected XmlElement ValueElement;
        protected XmlElement ParentElement;

        protected XmlNode ParameterNode;
        protected string ScopeName;
        private string _value;

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

            if (Value == null && ValueElement.ParentNode != null)
            {
                ParentElement.RemoveChild(ValueElement);
            }

            if (Value != null && ValueElement.ParentNode == null)
            {
                ParentElement.AppendChild(ValueElement);
            }
            if (Value != null)
                ValueElement.InnerText = Value;
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
