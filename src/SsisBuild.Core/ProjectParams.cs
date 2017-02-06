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
        protected override IList<IParameter> ExtractParameters()
        {
            var parameterNodes = FileXmlDocument.SelectNodes("/SSIS:Parameters/SSIS:Parameter", NamespaceManager);

            if (parameterNodes == null || parameterNodes.Count == 0)
                return null;

            var parameters = new List<IParameter>();

            foreach (XmlNode parameterNode in parameterNodes)
            {
                parameters.Add(new ProjectParameter("Project", parameterNode));
            }

            return parameters;
        }
    }
}
