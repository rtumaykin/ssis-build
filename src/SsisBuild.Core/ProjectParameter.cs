using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SsisBuild.Core.Helpers;

namespace SsisBuild.Core
{
    public sealed class ProjectParameter : Parameter
    {
        public ProjectParameter(string scopeName, XmlNode parameterNode) : base(scopeName, parameterNode, ParameterSource.Original)
        {
           InitializeFromXml();
        }

        protected override void InitializeFromXml()
        {
            if (ScopeName == null)
                throw new ArgumentNullException(nameof(ScopeName));

            var namespaceManager = ParameterNode.GetNameSpaceManager();
            var name = ParameterNode.GetAttribute("SSIS:Name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidXmlException("SSIS:Name attribute can not be null or empty", ParameterNode);

            var propertiesXmlNode = ParameterNode.SelectSingleNode("./SSIS:Properties", namespaceManager) as XmlElement;
            if (propertiesXmlNode == null)
                throw new InvalidXmlException("Could not find collection of parameter properties", ParameterNode);

            var valueXmlNode = propertiesXmlNode.SelectSingleNode("./SSIS:Property[@SSIS:Name = \"Value\"]", namespaceManager) as XmlElement;
            var value = valueXmlNode?.InnerText;

            Name = $"{ScopeName}::{name}";
            Value = value;
            ParentElement = propertiesXmlNode;
            ValueElement = valueXmlNode;
            Sensitive = ParentElement.SelectSingleNode("./SSIS:Property[@SSIS:Name = \"Sensitive\"]", ParentElement.GetNameSpaceManager())?.InnerText == "1";

            if (valueXmlNode == null)
            {
                ValueElement = ParentElement.GetDocument().CreateElement("SSIS:Property", XmlHelpers.Schemas.SSIS);
                ValueElement.SetAttribute("Name", XmlHelpers.Schemas.SSIS, "Value");
                if (Sensitive)
                    ValueElement.SetAttribute("Sensitive", XmlHelpers.Schemas.SSIS, "1");
            }
            else
            {
                ValueElement = valueXmlNode;
            }

            ParameterDataType = ExtractDataType();
        }

        private Type ExtractDataType()
        {
            DataType parameterDataType;
            if (!Enum.TryParse(ParentElement.SelectSingleNode("./SSIS:Property[@SSIS:Name = \"DataType\"]", ParentElement.GetNameSpaceManager())?.InnerText, out parameterDataType))
                return null;

            switch (parameterDataType)
            {
                case DataType.Boolean:
                    return typeof(bool);

                case DataType.Byte:
                    return typeof(byte);

                case DataType.DateTime:
                    return typeof(DateTime);

                case DataType.Decimal:
                    return typeof(decimal);

                case DataType.Double:
                    return typeof(double);

                case DataType.Int16:
                    return typeof(short);

                case DataType.Int32:
                    return typeof(int);

                case DataType.Int64:
                    return typeof(long);

                case DataType.SByte:
                    return typeof(sbyte);

                case DataType.Single:
                    return typeof(float);

                case DataType.String:
                    return typeof(string);

                case DataType.UInt32:
                    return typeof(uint);

                case DataType.UInt64:
                    return typeof(ulong);

                default:
                    return null;
            }
        }
    }
}
