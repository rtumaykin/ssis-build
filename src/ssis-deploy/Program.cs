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
using System.IO;
using System.Linq;
using SsisBuild.Core;

namespace SsisDeploy
{
    class Program
    {
        private enum ObjectTypes : short
        {
            Project = 20,
            Package = 30
        }

        private class SensitiveParameter
        {
            public string Name { get; set; }
            public Type DataType { get; set; }
            public string Value { get; set; }
        }

        static void Main(string[] args)
        {
            try
            {
                var deploymentArguments = DeployArguments.ProcessArgs(args);

                var project = Project.LoadFromIspac(deploymentArguments.DeploymentFilePath, deploymentArguments.ProjectPassword);

                var sensitiveParameters = project.Parameters.Where(p => p.Value.Sensitive && p.Value.Value != null)
                    .ToDictionary(p => string.Copy(p.Key), v => new SensitiveParameter()
                    {
                        DataType = v.Value.ParameterDataType,
                        Name = string.Copy(v.Key),
                        Value = string.Copy(v.Value.Value)
                    });


                var deploymentProtectionLevel = deploymentArguments.EraseSensitiveInfo ? ProtectionLevel.DontSaveSensitive : ProtectionLevel.ServerStorage;

                var cs = new SqlConnectionStringBuilder()
                {
                    ApplicationName = "SSIS Deploy",
                    DataSource = deploymentArguments.ServerInstance,
                    InitialCatalog = deploymentArguments.Catalog,
                    IntegratedSecurity = true
                };

                using (var zipStream = new MemoryStream())
                {
                    project.Save(zipStream, deploymentProtectionLevel, deploymentArguments.ProjectPassword);
                    zipStream.Flush();



                    Data.ExecutionScope.ConnectionString = cs.ConnectionString;

                    try
                    {
                        Data.Executables.catalog.create_folder.Execute(deploymentArguments.Folder, null);
                    }
                    catch (SqlException e)
                    {
                        // Ignore if the folder already there
                        if (e.Number != 27190)
                            throw;
                    }

                    Data.Executables.catalog.deploy_project.Execute(deploymentArguments.Folder, deploymentArguments.ProjectName, zipStream.ToArray(), null);

                    if (deploymentArguments.EraseSensitiveInfo)
                    {
                        foreach (var sensitiveParameter in sensitiveParameters)
                        {
                            var parameterSplit = sensitiveParameter.Key.Split(new[] {"::"}, StringSplitOptions.None);
                            if (parameterSplit.Length == 2)
                            {
                                var objectType = parameterSplit[0].ToLowerInvariant() == "project" ? ObjectTypes.Project : ObjectTypes.Package;
                                var value = ConvertToObject(sensitiveParameter.Value.Value, sensitiveParameter.Value.DataType);
                                Data.Executables.catalog.set_object_parameter_value.Execute(
                                    (short) objectType,
                                    deploymentArguments.Folder,
                                    deploymentArguments.ProjectName,
                                    parameterSplit[1],
                                    value,
                                    objectType == ObjectTypes.Project ? parameterSplit[0] : null,
                                    "V");
                            }
                        }
                    }
                }
            }
            catch (InvalidArgumentException x)
            {
                Console.WriteLine(x.Message);
                Usage();
                Environment.Exit(1);
            }
            catch (Exception e)
            {

                Environment.Exit(1);
            }
        }

        private static void Usage()
        {
            
        }

        private static object ConvertToObject(string value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFromString(value);
        }
    }
}
