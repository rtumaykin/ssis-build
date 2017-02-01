using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
