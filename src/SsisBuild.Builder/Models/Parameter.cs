using System;
using System.Xml;
using SsisBuild.Helpers;

namespace SsisBuild.Models
{
    public class Parameter
    {
        public string Name { get; }
        public string Value { get; private set; }
        public ParameterSource Source { get; private set; }

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

            if (valueElement == null)
            {
                _valueElement = _parentElement.GetDocument().CreateElement("SSIS:Property", XmlHelpers.Schemas.SSIS);
                _valueElement.SetAttribute("Name", XmlHelpers.Schemas.SSIS, "Value");
                var sensitive = _parentElement.SelectSingleNode("./SSIS:Property[@SSIS:Name = \"Sensitive\"]", _parentElement.GetNameSpaceManager())?.InnerText;
                if (sensitive == "1")
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
