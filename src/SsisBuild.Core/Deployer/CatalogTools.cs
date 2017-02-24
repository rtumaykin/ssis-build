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
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SsisBuild.Core.Deployer.Sql;

namespace SsisBuild.Core.Deployer
{
    [ExcludeFromCodeCoverage]
    public class CatalogTools : ICatalogTools
    {
        public void DeployProject(string connectionString, string folderName, string projectName, bool eraseSensitiveInfo, IDictionary<string, SensitiveParameter> parametersToDeploy, MemoryStream projectStream)
        {
            ExecutionScope.ConnectionString = connectionString;

            try
            {
                CreateFolder.Execute(folderName, null);
            }
            catch (SqlException e)
            {
                // Ignore if the folder already there
                if (e.Number != 27190)
                    throw;
            }

            Sql.DeployProject.Execute(folderName, projectName, projectStream.ToArray(), null);

            if (eraseSensitiveInfo)
            {
                foreach (var parameterToDeploy in parametersToDeploy)
                {
                    var parameterSplit = parameterToDeploy.Key.Split(new[] { "::" }, StringSplitOptions.None);
                    if (parameterSplit.Length == 2)
                    {
                        var objectType = parameterSplit[0].ToLowerInvariant() == "project" ? ObjectTypes.Project : ObjectTypes.Package;
                        var value = ConvertToObject(parameterToDeploy.Value.Value, parameterToDeploy.Value.DataType);
                        SetObjectParameterValue.Execute(
                            (short)objectType,
                            folderName,
                            projectName,
                            parameterSplit[1],
                            value,
                            objectType == ObjectTypes.Project ? parameterSplit[0] : null,
                            "V");
                    }
                }
            }
        }

        private static object ConvertToObject(string value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFromString(value);
        }
    }
}