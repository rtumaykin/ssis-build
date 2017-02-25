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

using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using SsisBuild.Core.ProjectManagement;
using SsisBuild.Logger;

namespace SsisBuild.Core.Deployer
{
    public class Deployer : IDeployer
    {
        private readonly ILogger _logger;
        private readonly IProject _project;
        private readonly ICatalogTools _catalogTools;

        internal Deployer(ILogger logger, IProject project, ICatalogTools catalogTools)
        {
            _logger = logger;
            _project = project;
            _catalogTools = catalogTools;
        }

        public Deployer() : this(new ConsoleLogger(), new Project(), new CatalogTools()) { }

        public void Deploy(IDeployArguments deployArguments)
        {
            string deploymentFilePath;

            if (string.IsNullOrWhiteSpace(deployArguments.DeploymentFilePath))
            {
                deploymentFilePath = Directory.EnumerateFiles(deployArguments.WorkingFolder, "*.ispac").FirstOrDefault();
                if (string.IsNullOrWhiteSpace(deploymentFilePath))
                {
                    throw new DeploymentFileNotFoundException(deployArguments.WorkingFolder);
                }
            }
            else
            {
                deploymentFilePath = Path.GetFullPath(
                    Path.IsPathRooted(deployArguments.DeploymentFilePath)
                        ? deployArguments.DeploymentFilePath
                        : Path.Combine(deployArguments.WorkingFolder, deployArguments.DeploymentFilePath)
                );
            }

            var catalog = deployArguments.Catalog ?? "SSISDB";

            var projectName = deployArguments.ProjectName ?? Path.GetFileNameWithoutExtension(deploymentFilePath);

            _project.LoadFromIspac(deploymentFilePath, deployArguments.ProjectPassword);

            var parametersToDeploy = _project.Parameters.Where(p => p.Value.Sensitive && p.Value.Value != null)
                .ToDictionary(p => string.Copy(p.Key), v => new SensitiveParameter(string.Copy(v.Key), string.Copy(v.Value.Value), v.Value.ParameterDataType));


            var deploymentProtectionLevel = deployArguments.EraseSensitiveInfo ? ProtectionLevel.DontSaveSensitive : ProtectionLevel.ServerStorage;

            LogDeploymentInfo(
                new DeployArguments(
                    deployArguments.WorkingFolder, 
                    deploymentFilePath, 
                    deployArguments.ServerInstance, 
                    catalog, deployArguments.Folder, 
                    projectName, 
                    deployArguments.ProjectPassword, 
                    deployArguments.EraseSensitiveInfo
                ), 
                parametersToDeploy, 
                deploymentProtectionLevel);

            var connectionString = new SqlConnectionStringBuilder()
            {
                ApplicationName = "SSIS Deploy",
                DataSource = deployArguments.ServerInstance,
                InitialCatalog = catalog,
                IntegratedSecurity = true
            }.ConnectionString;

            using (var zipStream = new MemoryStream())
            {
                _project.Save(zipStream, deploymentProtectionLevel, deployArguments.ProjectPassword);
                zipStream.Flush();

                _catalogTools.DeployProject(connectionString, deployArguments.Folder, projectName, deployArguments.EraseSensitiveInfo, parametersToDeploy, zipStream);
            }

            _logger.LogMessage("");
            _logger.LogMessage("Deployment completed successfully");
        }

        private void LogDeploymentInfo(IDeployArguments deployArguments, Dictionary<string, SensitiveParameter> parametersToDeploy, ProtectionLevel deploymentProtectionLevel)
        {
            _logger.LogMessage("SSIS Deploy Engine");
            _logger.LogMessage("Copyright (c) 2017 Roman Tumaykin");
            _logger.LogMessage("");
            _logger.LogMessage("-------------------------------------------------------------------------------");
            _logger.LogMessage("Starting SSIS Project deployment with the following parameters:");
            _logger.LogMessage("");
            _logger.LogMessage($"Project path:         {deployArguments.DeploymentFilePath}.");
            _logger.LogMessage($"Target SQL Server:    {deployArguments.ServerInstance}");
            _logger.LogMessage($"Target IS Catalog:    {deployArguments.Catalog}");
            _logger.LogMessage($"Target Project Name:  {deployArguments.ProjectName}");
            _logger.LogMessage($"Protection Level:     {deploymentProtectionLevel:G}");

            if (parametersToDeploy.Count > 0)
            {
                _logger.LogMessage("");
                _logger.LogMessage("The following parameters will be deployed together with the project:");
                foreach (var parameter in parametersToDeploy)
                {
                    _logger.LogMessage($"    - {parameter.Key};");
                }
            }
        }
    }
}