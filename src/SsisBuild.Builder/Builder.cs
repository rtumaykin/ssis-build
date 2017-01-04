using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.DataTransformationServices.Project;
using Microsoft.DataTransformationServices.Project.ComponentModel;
using Microsoft.DataTransformationServices.Project.Serialization;
using Microsoft.DataWarehouse.VsIntegration.Shell.Project.Configuration;
using Microsoft.SqlServer.Dts.Runtime;
using SsisBuild.Logger;

namespace SsisBuild
{
    public class Builder
    {
        private readonly ILogger _logger;

        public Builder(ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            _logger = logger;
        }

        /// <summary>
        /// Builds an ispac file based on dtproj file and overrides
        /// </summary>
        /// <param name="projectFilePath">A full path to a dtproj file</param>
        /// <param name="outputDirectory">Output directory where an ispac file will be created.</param>
        /// <param name="protectionLevel"></param>
        /// <param name="password"></param>
        /// <param name="newPassword"></param>
        /// <param name="configurationName"></param>
        /// <param name="parameters"></param>
        public void Execute(
            string projectFilePath,
            string protectionLevel,
            string password,
            string newPassword,
            string outputDirectory,
            string configurationName,
            IDictionary<string, string> parameters
        )
        {
            var sourceProjectDirectory = Path.GetDirectoryName(projectFilePath);

            if (!File.Exists(projectFilePath))
            {
                _logger.LogError($"Project file {projectFilePath} does not exist.");
                return;
            }

            _logger.LogMessage($"----------- Starting Build. Project: {projectFilePath} ------------------------");

            var sourceProject = DeserializeDtproj(projectFilePath);

            if (sourceProject.DeploymentModel != DeploymentModel.Project)
            {
                _logger.LogError("This task only apply to the SSIS project deployment model");
                return;
            }

            var projectManifest = ExtractProjectManifest(sourceProject);

            var configuration = GetProjectConfiguration(sourceProject, configurationName);

            var outputFileName = Path.GetFileName(Path.ChangeExtension(projectFilePath, "ispac"));
            var outputFileDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                ? Path.Combine(sourceProjectDirectory, "bin", configuration.Name)
                : outputDirectory;

            Directory.CreateDirectory(outputFileDirectory);

            var outputFilePath = Path.Combine(outputFileDirectory, outputFileName);

            _logger.LogMessage($"Setting output file path to {outputFilePath}");

            var outputProject = Project.CreateProject();

            outputProject.TargetServerVersion = configuration.Options.TargetServerVersion;

            outputProject.OfflineMode = true;

            SetProjectPropertiesFromManifest(outputProject, projectManifest);

            // read and assign project parameters
            {
                var projectParametersFilePath = Path.Combine(sourceProjectDirectory, "Project.params");
                var projectParamsXml = new XmlDocument();
                projectParamsXml.Load(projectParametersFilePath);

                var projectParamsXmlNamespaceManager = new XmlNamespaceManager(projectParamsXml.NameTable);
                projectParamsXmlNamespaceManager.AddNamespace("SSIS", Constants.NsSsis);
                var projectParamsParameterNodes = projectParamsXml.SelectNodes("//SSIS:Parameter",
                    projectParamsXmlNamespaceManager);

                if (projectParamsParameterNodes != null)
                {
                    foreach (var projectParamsParameterNode in projectParamsParameterNodes)
                    {
                        var projectParamsParameterXmlNode = projectParamsParameterNode as XmlNode;

                        if (projectParamsParameterXmlNode?.Attributes != null)
                        {
                            var parameterName = projectParamsParameterXmlNode.Attributes["SSIS:Name"].Value;

                            _logger.LogMessage($"Adding Project Parameter{parameterName}.");

                            var parameter = outputProject.Parameters.Add(parameterName, TypeCode.Boolean);
                            parameter.LoadFromXML(projectParamsParameterXmlNode, new DefaultEvents());
                        }
                    }
                }

                // Read parameter values to be assigned from configuration
                var parameterSet = new Dictionary<string, ConfigurationSetting>();
                foreach (string key in configuration.Options.ParameterConfigurationValues.Keys)
                {
                    // check if it's a GUID
                    Guid guid;
                    if (Guid.TryParse(key, out guid))
                    {
                        var setting = configuration.Options.ParameterConfigurationValues[key];
                        _logger.LogMessage($"Setting {key} to value {setting.Value}");
                        parameterSet.Add(key, setting);
                    }
                }

                // assign parameter values for real
                SetParameterConfigurationValues(outputProject.Parameters, parameterSet);

                // Add connections to project
                foreach (var connectionManagerName in projectManifest.ConnectionManagers)
                {
                    var connectionManagerFilePath = Path.Combine(sourceProjectDirectory, connectionManagerName); 
                    _logger.LogMessage($"Loading Connection Manager {connectionManagerFilePath}.");
                    var connectionManagerXml = new XmlDocument();
                    connectionManagerXml.Load(connectionManagerFilePath);
                    var nsManager = new XmlNamespaceManager(connectionManagerXml.NameTable);
                    nsManager.AddNamespace("DTS", Constants.NsDts);
                    var connectionManagerXmlNode = connectionManagerXml.SelectSingleNode("DTS:ConnectionManager", nsManager) as XmlNode;
                    if (connectionManagerXmlNode?.Attributes != null && (connectionManagerXmlNode.Attributes.Count > 0))
                    {
                        var creationName = connectionManagerXmlNode.Attributes["DTS:CreationName"].Value;
                        var connectionManager = outputProject.ConnectionManagerItems.Add(creationName, connectionManagerName);
                        connectionManager.Load(null, File.OpenRead(connectionManagerFilePath));
                    }
                }

                // Add packages to project
                foreach (var packageItem in projectManifest.Packages)
                {
                    var packagePath = Path.Combine(sourceProjectDirectory, packageItem.Name);
                    var package = LoadPackage(packagePath);
                    package.ProtectionLevel = DTSProtectionLevel.DontSaveSensitive;

                    // set package parameters
                    if (parameterSet.Count > 0)
                    {
                        SetParameterConfigurationValues(package.Parameters, parameterSet);
                    }

                    outputProject.PackageItems.Add(package, packageItem.Name);
                    outputProject.PackageItems[packageItem.Name].EntryPoint = packageItem.EntryPoint;
                    outputProject.PackageItems[packageItem.Name].Package.ComputeExpressions(true);
                }


                // Save project
                _logger.LogMessage($"Saving project to: {outputFilePath}.");

                outputProject.SaveTo(outputFilePath);
                outputProject.Dispose();
            }
        }

        private Package LoadPackage(string packagePath)
        {
            Package package;

            _logger.LogMessage($"Loading package {packagePath}.");
            try
            {
                var xml = File.ReadAllText(packagePath);

                package = new Package
                {
                    IgnoreConfigurationsOnLoad = true,
                    CheckSignatureOnLoad = false,
                    OfflineMode = true
                };
                package.LoadFromXML(xml, null);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while loading package {packagePath} : {e.Message}.");
                return null;
            }

            return package;
        }

        private void SetParameterConfigurationValues(Parameters parameters,
            IDictionary<string, ConfigurationSetting> set)
        {
            foreach (Parameter parameter in parameters)
            {
                if (set.ContainsKey(parameter.ID))
                {
                    var configSetting = set[parameter.ID];
                    parameter.Value = configSetting.Value;

                    _logger.LogMessage($"Setting value for {configSetting.Name}.");

                    // remove parameter
                    set.Remove(parameter.ID);

                    if (set.Count == 0)
                    {
                        break;
                    }
                }
            }
        }

        private ProjectSerialization DeserializeDtproj(string project)
        {
            _logger.LogMessage($"Loading project information from {project}.");

            var xmlOverrides = new XmlAttributeOverrides();
            ProjectConfigurationOptions.PrepareSerializationOverrides(
                typeof(DataTransformationsProjectConfigurationOptions), SerializationLevel.Project, xmlOverrides);

            // Read project file
            var serializer = new XmlSerializer(typeof(ProjectSerialization), xmlOverrides);
            var fileStream = File.OpenRead(project);
            return serializer.Deserialize(fileStream) as ProjectSerialization;
        }

        /// <summary>
        /// Reads Project Manifest from deserialized project
        /// </summary>
        /// <param name="sourceProject"></param>
        /// <returns></returns>
        private ProjectManifest ExtractProjectManifest(ProjectSerialization sourceProject)
        {
            _logger.LogMessage("Extracting Project Manifest. Forcing the Project Security to \"Do Not Save Sensitive Information\".");

            var manifest = new ProjectManifest()
            {
                // for builds there is no need to save any sensitive info. 
                // If it exists it should be replaced or added by the build server
                ProtectionLevel = DTSProtectionLevel.DontSaveSensitive,
                Properties = new Dictionary<string, string>(),
                Packages = new List<PackageManifest>(),
                ConnectionManagers = new List<string>()
            };

            var manifestXml = new XmlDocument();
            manifestXml.LoadXml(sourceProject.DeploymentModelSpecificXmlNode.InnerXml);
            var nsManager = new XmlNamespaceManager(manifestXml.NameTable);
            nsManager.AddNamespace("SSIS", Constants.NsSsis);

            // Properties
            var propertyNodes = manifestXml.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property",
                nsManager);

            if (propertyNodes != null)
            {
                foreach (var propertyNode in propertyNodes)
                {
                    var propertyXmlNode = propertyNode as XmlNode;
                    if (propertyXmlNode?.Attributes != null && propertyXmlNode.Attributes.Count > 0)
                    {
                        var propertyName = propertyXmlNode.Attributes["SSIS:Name"].Value;
                        var propertyValue = propertyXmlNode.HasChildNodes
                            ? propertyXmlNode.ChildNodes.OfType<XmlNode>()
                                .FirstOrDefault(n => n.NodeType == XmlNodeType.CDATA || n.NodeType == XmlNodeType.Text)?
                                .Value
                            : "";

                        manifest.Properties.Add(propertyName, propertyValue);
                    }
                }
            }

            // Packages
            var packageNodes = manifestXml.SelectNodes(
                "/SSIS:Project/SSIS:Packages/SSIS:Package", nsManager);

            if (packageNodes != null)
            {
                foreach (var packageNode in packageNodes)
                {
                    var packageXmlNode = packageNode as XmlNode;
                    if (packageXmlNode?.Attributes != null && packageXmlNode.Attributes.Count > 0)
                    {
                        var packageName = packageXmlNode.Attributes["SSIS:Name"].Value;
                        var entryPointString = packageXmlNode.Attributes["SSIS:EntryPoint"].Value;
                        bool entryPoint;
                        if (new[] {"0", "1"}.Contains(entryPointString))
                        {
                            entryPoint = entryPointString != "0";
                        }
                        else
                        {
                            if (!bool.TryParse(entryPointString, out entryPoint))
                            {
                                continue;
                            }
                        }

                        manifest.Packages.Add(new PackageManifest() {EntryPoint = entryPoint, Name = packageName});
                    }
                }
            }

            // Connection managers
            var connectionManagerNodes = manifestXml.SelectNodes(
                "/SSIS:Project/SSIS:ConnectionManagers/SSIS:ConnectionManager", nsManager);

            if (connectionManagerNodes != null)
            {
                foreach (var connectionManagerNode in connectionManagerNodes)
                {
                    var connectionManagerXmlNode = connectionManagerNode as XmlNode;
                    if (connectionManagerXmlNode?.Attributes != null && connectionManagerXmlNode.Attributes.Count > 0)
                    {
                        var connectionManagerName = connectionManagerXmlNode.Attributes["SSIS:Name"].Value;
                        manifest.ConnectionManagers.Add(connectionManagerName);
                    }
                }
            }

            return manifest;
        }

        private DataTransformationsConfiguration GetProjectConfiguration(ProjectSerialization sourceProject, string configurationName)
        {
            _logger.LogMessage($"Extracting configuration {configurationName}");

            foreach (var configurationObject in sourceProject.Configurations)
            {
                var configuration = configurationObject as DataTransformationsConfiguration;
                if (configuration != null && configuration.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase))
                {
                    return configuration;
                }
            }

            var firstConfiguration = sourceProject.Configurations[0] as DataTransformationsConfiguration;
            _logger.LogMessage($"Creating new configuration {configurationName} from existing configuration {firstConfiguration.Name}.");
            firstConfiguration.Name = configurationName;

            return firstConfiguration;
        }

        private static void SetProjectPropertiesFromManifest(Project project, ProjectManifest manifest)
        {
            // set the properties we care about
            foreach (var property in manifest.Properties.Keys)
            {
                switch (property)
                {
                    case "Name":
                        project.Name = manifest.Properties[property];
                        break;
                    case "VersionMajor":
                        project.VersionMajor = int.Parse(manifest.Properties[property]);
                        break;
                    case "VersionMinor":
                        project.VersionMinor = int.Parse(manifest.Properties[property]);
                        break;
                    case "VersionBuild":
                        project.VersionBuild = int.Parse(manifest.Properties[property]);
                        break;
                    case "VersionComments":
                        project.VersionComments = manifest.Properties[property];
                        break;
                }
            }
        }
    }
}
