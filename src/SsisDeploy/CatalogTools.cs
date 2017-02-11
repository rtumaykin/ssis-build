using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;

namespace SsisDeploy
{
    public class CatalogTools : ICatalogTools
    {
        public void DeployProject(string connectionString, string folderName, string projectName, bool eraseSensitiveInfo, IDictionary<string, SensitiveParameter> parametersToDeploy, MemoryStream projectStream)
        {
            Data.ExecutionScope.ConnectionString = connectionString;

            try
            {
                Data.Executables.catalog.create_folder.Execute(folderName, null);
            }
            catch (SqlException e)
            {
                // Ignore if the folder already there
                if (e.Number != 27190)
                    throw;
            }

            Data.Executables.catalog.deploy_project.Execute(folderName, projectName, projectStream.ToArray(), null);

            if (eraseSensitiveInfo)
            {
                foreach (var parameterToDeploy in parametersToDeploy)
                {
                    var parameterSplit = parameterToDeploy.Key.Split(new[] { "::" }, StringSplitOptions.None);
                    if (parameterSplit.Length == 2)
                    {
                        var objectType = parameterSplit[0].ToLowerInvariant() == "project" ? ObjectTypes.Project : ObjectTypes.Package;
                        var value = ConvertToObject(parameterToDeploy.Value.Value, parameterToDeploy.Value.DataType);
                        Data.Executables.catalog.set_object_parameter_value.Execute(
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