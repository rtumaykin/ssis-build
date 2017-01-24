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
    public class Parameter
    {
        public string Name { get; }
        public string Value { get; private set; }
        public ParameterSource Source { get; private set; }
        public bool Sensitive { get; private set; }

        private readonly XmlElement _valueElement;
        private readonly XmlElement _parentElement;

        public Parameter(string name, string value, ParameterSource source, XmlElement valueElement) : this(name, value, source, valueElement, valueElement.ParentNode as XmlElement)
        {
            
        }

        public Parameter(string name, string value, ParameterSource source, XmlElement valueElement, XmlElement parentElement)
        {
            if (parentElement == null)
                throw new ArgumentNullException(nameof(parentElement));

            if (_valueElement != null && _parentElement != null && _valueElement.ParentNode != _parentElement)
                throw new Exception("Parent element does not match the value parent.");

            Name = name;
            Value = value;
            Source = source;

            _parentElement = parentElement;
            Sensitive = _parentElement.SelectSingleNode("./SSIS:Property[@SSIS:Name = \"Sensitive\"]", _parentElement.GetNameSpaceManager())?.InnerText == "1";

            if (valueElement == null)
            {
                _valueElement = _parentElement.GetDocument().CreateElement("SSIS:Property", XmlHelpers.Schemas.SSIS);
                _valueElement.SetAttribute("Name", XmlHelpers.Schemas.SSIS, "Value");
                if (Sensitive)
                    _valueElement.SetAttribute("Sensitive", XmlHelpers.Schemas.SSIS, "1");
            }
            else
                _valueElement = valueElement;
        }

        public void SetValue(string value, ParameterSource source)
        {
            Value = value;
            Source = source;

            if (value == null && _valueElement.ParentNode != null)
            {
                _parentElement.RemoveChild(_valueElement);
            }

            if (value != null && _valueElement.ParentNode == null)
            {
                _parentElement.AppendChild(_valueElement);
            }
            if (value != null)
                _valueElement.InnerText = value;
            else
            {
                foreach (XmlNode childNode in _valueElement.ChildNodes)
                {
                    if (childNode.NodeType != XmlNodeType.Attribute)
                        _valueElement.RemoveChild(childNode);
                }
            }
        }
    }
}
