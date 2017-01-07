using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// <param name="releaseNotesFilePath"></param>
        /// <param name="parameters"></param>
        /// <param name="sensitiveParameters"></param>
        public void Execute(
            string projectFilePath,
            string protectionLevel,
            string password,
            string newPassword,
            string outputDirectory,
            string configurationName,
            string releaseNotesFilePath,
            IDictionary<string, string> parameters,
            IDictionary<string, string> sensitiveParameters 
        )
        {
            try
            {

                // join sensitive parameters
                if (parameters == null)
                {
                    parameters = sensitiveParameters;
                }
                else if (sensitiveParameters != null)
                {
                    foreach (var sensitiveParameter in sensitiveParameters)
                    {
                        parameters.Add(sensitiveParameter.Key, sensitiveParameter.Value);
                    }
                }

                LogParametersUsed(projectFilePath, protectionLevel, password, newPassword, outputDirectory, configurationName, parameters, sensitiveParameters);

                var sourceProjectDirectory = Path.GetDirectoryName(projectFilePath);

                if (string.IsNullOrWhiteSpace(sourceProjectDirectory) || !Directory.Exists(sourceProjectDirectory))
                    throw new Exception($"Empty or invalid project directory {sourceProjectDirectory}");

                if (!File.Exists(projectFilePath))
                    throw new Exception($"Project file {projectFilePath} does not exist.");

                _logger.LogMessage("");
                _logger.LogMessage($"----------- Starting Build. Project file: {projectFilePath} ------------------------");

                var sourceProject = DeserializeDtproj(projectFilePath);

                if (sourceProject.DeploymentModel != DeploymentModel.Project)
                {
                    _logger.LogError("This task only apply to the SSIS project deployment model. Exiting.");
                    return;
                }

                var projectManifest = ExtractProjectManifest(sourceProject, password);

                var configuration = GetProjectConfiguration(sourceProject, configurationName);
                MergeUserOptions(projectFilePath, configuration);

                var outputFileName = Path.GetFileName(Path.ChangeExtension(projectFilePath, "ispac"));

                if (string.IsNullOrWhiteSpace(outputFileName))
                {
                    _logger.LogError("failed to set output file name.");
                    return;
                }
                var outputFileDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                    ? Path.Combine(sourceProjectDirectory, "bin", configuration.Name)
                    : outputDirectory;

                Directory.CreateDirectory(outputFileDirectory);

                var outputFilePath = Path.Combine(outputFileDirectory, outputFileName);

                _logger.LogMessage($"Setting output file path to {outputFilePath}");

                var outputProject = Project.CreateProject();

                _logger.LogMessage($"Project Target Version is set to {configuration.Options.TargetServerVersion}.");
                outputProject.TargetServerVersion = configuration.Options.TargetServerVersion;

                outputProject.OfflineMode = true;

                SetProjectPropertiesFromManifest(outputProject, projectManifest);

                // dealing with encryption
                var finalProtectionLevel = string.IsNullOrWhiteSpace(protectionLevel)
                    ? projectManifest.ProtectionLevel
                    : (DTSProtectionLevel) Enum.Parse(typeof(DTSProtectionLevel), protectionLevel, true);

                _logger.LogMessage($"Original project protection level is {projectManifest.ProtectionLevel}.");
                _logger.LogMessage($"Destination project protection level is {finalProtectionLevel}.");
                outputProject.ProtectionLevel = finalProtectionLevel;

                string encryptionPassword = null;

                if (IsPasswordProtectedLevel(finalProtectionLevel))
                {
                    encryptionPassword = IsPasswordProtectedLevel(projectManifest.ProtectionLevel)
                        ? (string.IsNullOrEmpty(newPassword) ? password : newPassword)
                        : newPassword;

                    if (string.IsNullOrWhiteSpace(encryptionPassword))
                    {
                        throw new Exception(
                            $"<NewPassword> switch is required to change project to Protection Level {finalProtectionLevel} from {projectManifest.ProtectionLevel}");
                    }

                    if (encryptionPassword == password)
                    {
                        _logger.LogMessage("Using the original password to encrypt project contents.");
                    }

                    outputProject.Password = encryptionPassword;
                }


                // read and assign project parameters
                    var projectParametersFilePath = Path.Combine(sourceProjectDirectory, "Project.params");
                    var projectParamsXml = new XmlDocument();
                    projectParamsXml.Load(projectParametersFilePath);

                    if (IsPasswordProtectedLevel(projectManifest.ProtectionLevel))
                    {
                        Decryptor.DecryptXmlNode(projectParamsXml, projectManifest.ProtectionLevel, password);
                    }

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

                                _logger.LogMessage($"Adding Project Parameter {parameterName}.");

                                var parameter = outputProject.Parameters.Add(parameterName, TypeCode.Boolean);
                                parameter.LoadFromXML(projectParamsParameterXmlNode, new DefaultEvents());
                            // force sensitive
                            if (sensitiveParameters.ContainsKey(parameterName))
                                    parameter.Sensitive = true;

                                if (parameters.ContainsKey($"Project::{parameter.Name}"))
                                {
                                    parameter.Value = ConvertToObject(parameters[$"Project::{parameter.Name}"],
                                        parameter.Value.GetType());
                                    _logger.LogMessage(
                                        $"Value of {parameter.Name} is set based on passed parameter to {parameter.Value}.");

                                }
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
                        // Project parameters  only if it has not been assigned above from parameter
                        if (!parameters.ContainsKey(setting.Name) || !setting.Name.StartsWith("Project::"))
                        {
                            parameterSet.Add(key, setting);
                        }

                    }
                }

                // assign parameter values from configuration except for project parameters that have been assigned from build arguments
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
                        var connectionManagerXmlNode =
                            connectionManagerXml.SelectSingleNode("DTS:ConnectionManager", nsManager) as XmlNode;
                        if (connectionManagerXmlNode?.Attributes != null &&
                            (connectionManagerXmlNode.Attributes.Count > 0))
                        {
                            var creationName = connectionManagerXmlNode.Attributes["DTS:CreationName"].Value;
                            var connectionManager = outputProject.ConnectionManagerItems.Add(creationName,
                                connectionManagerName);
                            connectionManager.Load(null, File.OpenRead(connectionManagerFilePath));
                        }
                    

                    // Add packages to project
                    foreach (var packageItem in projectManifest.Packages)
                    {
                        var packagePath = Path.Combine(sourceProjectDirectory, packageItem.Name);
                        _logger.LogMessage($"Loading package {packagePath}");
                        var package = LoadPackage(packagePath);

                        _logger.LogMessage(
                            $"Original package {packageItem.Name} protection level is {package.ProtectionLevel}.");
                        _logger.LogMessage(
                            $"Setting package {packageItem.Name} protection level to {finalProtectionLevel}.");
                        package.ProtectionLevel = finalProtectionLevel;
                        if (IsPasswordProtectedLevel(finalProtectionLevel))
                        {
                            package.PackagePassword = encryptionPassword;
                        }

                        // set package parameters
                        if (parameterSet.Count > 0)
                        {
                            foreach (Parameter packageParameter in package.Parameters)
                            {
                                if (parameters.ContainsKey($"{package.Name}::{packageParameter.Name}"))
                                {
                                    _logger.LogMessage(
                                        $"Overriding parameter value for {packageParameter.Name} with passed parameter value {parameters[$"{package.Name}::{packageParameter.Name}"]}");
                                    packageParameter.Value =
                                        ConvertToObject(parameters[$"{package.Name}::{packageParameter.Name}"],
                                            packageParameter.Value.GetType());
                                }
                            }

                            SetParameterConfigurationValues(package.Parameters, parameterSet);
                        }


                        outputProject.PackageItems.Add(package, packageItem.Name);
                        outputProject.PackageItems[packageItem.Name].EntryPoint = packageItem.EntryPoint;
                        outputProject.PackageItems[packageItem.Name].Package.ComputeExpressions(true);
                    }

                    if (!string.IsNullOrWhiteSpace(releaseNotesFilePath))
                    {
                        if (File.Exists(releaseNotesFilePath))
                        {
                            try
                            {
                                var releaseNotes = ReleaseNotesHelper.ParseReleaseNotes(releaseNotesFilePath);
                                _logger.LogMessage($"Overriding Version to {releaseNotes.Version}");
                                outputProject.VersionMajor = releaseNotes.Version.Major;
                                outputProject.VersionMinor = releaseNotes.Version.Minor;
                                outputProject.VersionBuild = releaseNotes.Version.Build;

                                _logger.LogMessage($"Adding Release Notes {string.Join("\r\n", releaseNotes.Notes)}");
                                outputProject.VersionComments = string.Join("\r\n", releaseNotes.Notes);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"Failed with the following exception: {e.Message}");
                                return;
                            }
                        }
                        else
                        {
                            _logger.LogError($"Release notes file {releaseNotesFilePath} does not exist.");
                            return;
                        }
                    }

                    // Save project
                    _logger.LogMessage($"Saving project to: {outputFilePath}.");

                    outputProject.SaveTo(outputFilePath);
                    outputProject.Dispose();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        private void LogParametersUsed(string projectFilePath, string protectionLevel, string password, string newPassword,
            string outputDirectory, string configurationName, IDictionary<string, string> parameters, IDictionary<string, string> sensitiveParameters)
        {
            _logger.LogMessage("SSIS Build Engine");
            _logger.LogMessage("Copyright (c) 2017 Roman Tumaykin");
            _logger.LogMessage("");
            _logger.LogMessage("Executing SSIS Build with the following switches:");
            _logger.LogMessage($"Project File: {projectFilePath}");
            if (!string.IsNullOrWhiteSpace(protectionLevel))
            {
                _logger.LogMessage($"-ProtectionLevel: {protectionLevel}");
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                _logger.LogMessage($"-Password: {password}");
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                _logger.LogMessage($"-NewPassword: {newPassword}");
            }

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                _logger.LogMessage($"-OutputFolder: {outputDirectory}");
            }

            if (!string.IsNullOrWhiteSpace(configurationName))
            {
                _logger.LogMessage($"-Configuration: {configurationName}");
            }

            _logger.LogMessage("");
            _logger.LogMessage("Project parameters:");
            foreach (var parameter in parameters)
            {
                _logger.LogMessage(
                    $"  {parameter.Key} (Sensitive = {sensitiveParameters.Keys.Contains(parameter.Key)}): {parameter.Value}");
            }
        }

        private static object ConvertToObject(string value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFromString(value);
        }

        private static readonly DTSProtectionLevel[] PasswordProtectionLevels = {DTSProtectionLevel.EncryptAllWithPassword, DTSProtectionLevel.EncryptSensitiveWithPassword};

        private static bool IsPasswordProtectedLevel(DTSProtectionLevel protectionLevel)
        {
            return PasswordProtectionLevels.Contains(protectionLevel);
        }

        private static Package LoadPackage(string packagePath)
        {
            Package package;

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
                throw new Exception($"Error while loading package {packagePath} : {e.Message}.");
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

                    _logger.LogMessage($"Value of {parameter.Name} is set based on active configuration to {configSetting.Value}.");

                    // remove parameter
                    set.Remove(parameter.ID);

                    if (set.Count == 0)
                    {
                        break;
                    }
                }
            }
        }

        private static ProjectSerialization DeserializeDtproj(string project)
        {
            var xmlOverrides = new XmlAttributeOverrides();
            ProjectConfigurationOptions.PrepareSerializationOverrides(
                typeof(DataTransformationsProjectConfigurationOptions), SerializationLevel.Project, xmlOverrides);

            // Read project file
            var serializer = new XmlSerializer(typeof(ProjectSerialization), xmlOverrides);

            var fileStream = File.OpenRead(project);
            return serializer.Deserialize(fileStream) as ProjectSerialization;
        }

        /// <summary>
        /// Reads the dtproj.user file to find sensitive parameter that are included in configurations
        /// </summary>
        /// <param name="projectFilePath"></param>
        /// <param name="activeConfiguration"></param>
        /// <returns></returns>
        private static void MergeUserOptions(string projectFilePath, DataTransformationsConfiguration activeConfiguration)
        {
            var userProjectFilePath = $"{projectFilePath}.user";

            var xmlOverrides = new XmlAttributeOverrides();
            ProjectConfigurationOptions.PrepareSerializationOverrides(
                typeof(DataTransformationsProjectConfigurationOptions), SerializationLevel.User, xmlOverrides);

            // Read project file
            var serializer = new XmlSerializer(typeof(DataTransformationsUserConfiguration), xmlOverrides);

            var fileStream = File.OpenRead(userProjectFilePath);
            var deserializedUserProject = serializer.Deserialize(fileStream) as DataTransformationsUserConfiguration;

            var userOptions =
                deserializedUserProject?.Configurations.OfType<DataTransformationsConfiguration>()
                    .FirstOrDefault(c => c.Name == activeConfiguration.Name);

            if (userOptions == null)
                return;

            ProjectConfigurationOptions.AssignConfiguration(typeof(DataTransformationsProjectConfigurationOptions),
                userOptions.Options, activeConfiguration.Options,
                SerializationLevel.User);
        }

        /// <summary>
        /// Reads Project Manifest from deserialized project
        /// </summary>
        /// <param name="sourceProject"></param>
        /// <param name="decryptionPassword"></param>
        /// <returns></returns>
        private static ProjectManifest ExtractProjectManifest(ProjectSerialization sourceProject, string decryptionPassword)
        {
            var manifest = new ProjectManifest()
            {
                ProtectionLevel = DTSProtectionLevel.DontSaveSensitive,
                Properties = new Dictionary<string, string>(),
                Packages = new List<PackageManifest>(),
                ConnectionManagers = new List<string>()
            };

            var manifestXml = new XmlDocument();
            manifestXml.LoadXml(sourceProject.DeploymentModelSpecificXmlNode.InnerXml);
            var nsManager = new XmlNamespaceManager(manifestXml.NameTable);
            nsManager.AddNamespace("SSIS", Constants.NsSsis);

            // Protection Level
            var projectXmlNode = manifestXml.SelectSingleNode("/SSIS:Project", nsManager) as XmlNode;
            if (projectXmlNode?.Attributes != null && projectXmlNode.Attributes.Count > 0)
            {
                manifest.ProtectionLevel =
                    (DTSProtectionLevel)
                    Enum.Parse(typeof(DTSProtectionLevel), projectXmlNode.Attributes["SSIS:ProtectionLevel"].Value);
            }

            if (IsPasswordProtectedLevel(manifest.ProtectionLevel))
            {
                try
                {
                    Decryptor.DecryptXmlNode(manifestXml, manifest.ProtectionLevel, decryptionPassword);
                }
                catch (InvalidPaswordException)
                {
                    throw new Exception(string.IsNullOrWhiteSpace(decryptionPassword)
                        ? $"<Password> parameter is required to decrypt original project with protection level {manifest.ProtectionLevel}."
                        : "Invalid decryption password.");
                }
                catch (Exception e)
                {
                    throw new Exception($"Exception of type {e.GetType().FullName} occured: {e.Message}.");
                }
            }

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

        private static DataTransformationsConfiguration GetProjectConfiguration(ProjectSerialization sourceProject, string configurationName)
        {
            foreach (var configurationObject in sourceProject.Configurations)
            {
                var configuration = configurationObject as DataTransformationsConfiguration;
                if (configuration != null && configuration.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase))
                {
                    return configuration;
                }
            }

            throw new Exception($"Configuration {configurationName} does not exist in the project");
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
