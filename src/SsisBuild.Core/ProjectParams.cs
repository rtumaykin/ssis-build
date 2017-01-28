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
