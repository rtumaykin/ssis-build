using System;
using System.Collections.Generic;
using System.Xml;
using SsisBuild.Helpers;

namespace SsisBuild.Models
{
    public class ProjectParams : ProjectFile
    {
        protected override IList<Parameter> ExtractParameters()
        {
            var parameterNodes = FileXmlDocument.SelectNodes("/SSIS:Parameters/SSIS:Parameter", NamespaceManager);

            if (parameterNodes == null)
                return null;

            var parameters = new List<Parameter>();

            foreach (XmlNode parameterNode in parameterNodes)
            {
                var name = parameterNode?.Attributes?["SSIS:Name"]?.Value;
                if (string.IsNullOrWhiteSpace(name))
                    throw new Exception("Project parameter is missing SSIS:Name attribute value.");

                var fullName = $"Project::{name}";

                var parentElement = parameterNode.SelectSingleNode("./SSIS:Properties", NamespaceManager) as XmlElement;

                var valueElement = parentElement?.SelectSingleNode("./SSIS:Property[@SSIS:Name = \"Value\"]", NamespaceManager) as XmlElement;

                parameters.Add(new Parameter(fullName, valueElement?.InnerText, ParameterSource.Original, valueElement, parentElement));
            }

            return parameters;
        }
    }
}
