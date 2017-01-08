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
                LogParametersUsed(projectFilePath, protectionLevel, password, newPassword, outputDirectory,
                    configurationName, parameters, sensitiveParameters);

                var sourceProjectDirectory = Path.GetDirectoryName(projectFilePath);

                if (string.IsNullOrWhiteSpace(sourceProjectDirectory) || !Directory.Exists(sourceProjectDirectory))
                    throw new Exception($"Empty or invalid project directory {sourceProjectDirectory}");

                if (!File.Exists(projectFilePath))
                    throw new Exception($"Project file {projectFilePath} does not exist.");

                _logger.LogMessage("");
                _logger.LogMessage(
                    $"----------- Starting Build. Project file: {projectFilePath} ------------------------");

                var sourceProject = DeserializeDtproj(projectFilePath);

                if (sourceProject.DeploymentModel != DeploymentModel.Project)
                {
                    throw new Exception("This task only apply to the SSIS project deployment model. Exiting.");
                }

                var projectManifest = ExtractProjectManifest(sourceProject, password);

                var configuration = GetProjectConfiguration(sourceProject, configurationName);
                MergeUserOptions(projectFilePath, configuration);

                var consolidatedParameters = ConsolidateBuildParameters(parameters, sensitiveParameters);

                var outputFileName = Path.GetFileName(Path.ChangeExtension(projectFilePath, "ispac"));

                if (string.IsNullOrWhiteSpace(outputFileName))
                {
                    throw new Exception("failed to set output file name.");
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

                SetProjectProperties(outputProject, projectManifest.Properties);

                var finalProtectionLevel = GetFinalProtectionLevel(protectionLevel, projectManifest.ProtectionLevel);

                var encryptionPassword = GetEncryptionPassword(password, newPassword, finalProtectionLevel,
                    projectManifest);

                outputProject.ProtectionLevel = finalProtectionLevel;
                if (IsPasswordProtectedLevel(finalProtectionLevel))
                    outputProject.Password = encryptionPassword;

                AddProjectParameters(password, sourceProjectDirectory, projectManifest.ProtectionLevel, outputProject);

                UpdateConsolidatedParametersFromProjectParameters(outputProject.Parameters, consolidatedParameters);

                UpdateParameterDetailsFromConfiguration(configuration, consolidatedParameters);

                SetParameterFinalValues(outputProject.Parameters, consolidatedParameters, "Project");

                AddProjectConnections(projectManifest.ConnectionManagers, sourceProjectDirectory, outputProject);

                LoadPackages(password, projectManifest.Packages, sourceProjectDirectory, projectManifest.ProtectionLevel, finalProtectionLevel, encryptionPassword, consolidatedParameters, outputProject);

                SetVersionInfoFromReleaseNotes(releaseNotesFilePath, outputProject);

                // Save project
                _logger.LogMessage($"Saving project to: {outputFilePath}.");

                outputProject.SaveTo(outputFilePath);
                outputProject.Dispose();

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        private void LoadPackages(string passwordArgumentValue, IList<PackageManifest> packages, string sourceProjectDirectory,
            DTSProtectionLevel sourceProtectionLevel,
            DTSProtectionLevel finalProtectionLevel, string encryptionPassword,
            IList<ParameterDetail> consolidatedParameters,
            Project outputProject)
        {
            _logger.LogMessage("----------------------------------------------------------------------");

            // Add packages to project
            foreach (var packageItem in packages)
            {
                var packagePath = Path.Combine(sourceProjectDirectory, packageItem.Name);
                _logger.LogMessage($"Loading package {packagePath}");
                var package = LoadPackage(packagePath,
                    IsPasswordProtectedLevel(sourceProtectionLevel) ? passwordArgumentValue : null);

                if (package.ProtectionLevel != finalProtectionLevel)
                {
                    _logger.LogMessage(
                        $"Original package {packageItem.Name} protection level is {package.ProtectionLevel}.");
                    _logger.LogMessage(
                        $"Setting package {packageItem.Name} protection level to {finalProtectionLevel}.");
                }

                package.ProtectionLevel = finalProtectionLevel;
                if (IsPasswordProtectedLevel(finalProtectionLevel))
                {
                    package.PackagePassword = encryptionPassword;
                }

                SetParameterFinalValues(package.Parameters, consolidatedParameters, package.Name);


                outputProject.PackageItems.Add(package, packageItem.Name);
                outputProject.PackageItems[packageItem.Name].EntryPoint = packageItem.EntryPoint;
                outputProject.PackageItems[packageItem.Name].Package.ComputeExpressions(true);
            }
        }

        /// <summary>
        /// Parses release notes and assigns Version data
        /// </summary>
        /// <param name="releaseNotesFilePath"></param>
        /// <param name="outputProject"></param>
        private void SetVersionInfoFromReleaseNotes(string releaseNotesFilePath, Project outputProject)
        {
            if (!string.IsNullOrWhiteSpace(releaseNotesFilePath))
            {
                if (File.Exists(releaseNotesFilePath))
                {
                    var releaseNotes = ReleaseNotesHelper.ParseReleaseNotes(releaseNotesFilePath);
                    _logger.LogMessage($"Overriding Version to {releaseNotes.Version}");
                    outputProject.VersionMajor = releaseNotes.Version.Major;
                    outputProject.VersionMinor = releaseNotes.Version.Minor;
                    outputProject.VersionBuild = releaseNotes.Version.Build;

                    _logger.LogMessage($"Adding Release Notes {string.Join("\r\n", releaseNotes.Notes)}");
                    outputProject.VersionComments = string.Join("\r\n", releaseNotes.Notes);
                }
                else
                {
                    throw new Exception($"Release notes file {releaseNotesFilePath} does not exist.");
                }
            }
        }

        /// <summary>
        /// Determines the encryption password
        /// </summary>
        /// <param name="passwordParameterValue"></param>
        /// <param name="newPasswordParameterValue"></param>
        /// <param name="finalProtectionLevel"></param>
        /// <param name="projectManifest"></param>
        /// <returns></returns>
        private string GetEncryptionPassword(string passwordParameterValue, string newPasswordParameterValue,
            DTSProtectionLevel finalProtectionLevel,
            ProjectManifest projectManifest)
        {
            string encryptionPassword = null;

            if (IsPasswordProtectedLevel(finalProtectionLevel))
            {
                encryptionPassword = IsPasswordProtectedLevel(projectManifest.ProtectionLevel)
                    ? (string.IsNullOrEmpty(newPasswordParameterValue) ? passwordParameterValue : newPasswordParameterValue)
                    : newPasswordParameterValue;

                if (string.IsNullOrWhiteSpace(encryptionPassword))
                {
                    throw new Exception(
                        $"<NewPassword> switch is required to change project to Protection Level {finalProtectionLevel} from {projectManifest.ProtectionLevel}");
                }

                if (encryptionPassword == passwordParameterValue)
                {
                    _logger.LogMessage("Using the original password to encrypt project contents.");
                }
            }
            return encryptionPassword;
        }

        /// <summary>
        /// Determines the final protection level
        /// </summary>
        /// <param name="protectionLevelParameterValue"></param>
        /// <param name="sourceProtectionLevel"></param>
        /// <returns></returns>
        private DTSProtectionLevel GetFinalProtectionLevel(string protectionLevelParameterValue, DTSProtectionLevel sourceProtectionLevel)
        {
            var finalProtectionLevel = string.IsNullOrWhiteSpace(protectionLevelParameterValue)
                ? sourceProtectionLevel
                : (DTSProtectionLevel) Enum.Parse(typeof(DTSProtectionLevel), protectionLevelParameterValue, true);

            _logger.LogMessage($"Original project protection level is {sourceProtectionLevel}.");
            _logger.LogMessage($"Destination project protection level is {finalProtectionLevel}.");
            return finalProtectionLevel;
        }

        /// <summary>
        /// Reads project.params file and loads all parameters info into the output project
        /// </summary>
        /// <param name="decryptionPassword"></param>
        /// <param name="sourceProjectDirectory"></param>
        /// <param name="sourceProtectionLevel"></param>
        /// <param name="outputProject"></param>
        private void AddProjectParameters(string decryptionPassword, string sourceProjectDirectory,
            DTSProtectionLevel sourceProtectionLevel,
            Project outputProject)
        {
            // read and assign project parameters
            var projectParametersFilePath = Path.Combine(sourceProjectDirectory, "Project.params");
            var projectParamsXml = new XmlDocument();
            projectParamsXml.Load(projectParametersFilePath);

            if (IsPasswordProtectedLevel(sourceProtectionLevel))
            {
                Decryptor.DecryptXmlNode(projectParamsXml, sourceProtectionLevel, decryptionPassword);
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
                    }
                }
            }
        }

        /// <summary>
        /// Parses connection managers data and adds resulting info to the output project
        /// </summary>
        /// <param name="connectionManagers"></param>
        /// <param name="sourceProjectDirectory"></param>
        /// <param name="outputProject"></param>
        private void AddProjectConnections(IList<string> connectionManagers, string sourceProjectDirectory,
            Project outputProject)
        {
            // Add connections to project
            foreach (var connectionManagerName in connectionManagers)
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
            }
        }

        /// <summary>
        /// Updates Consolidated parameters with data extracted from active configuration
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="consolidatedParametersData"></param>
        private static void UpdateParameterDetailsFromConfiguration(DataTransformationsConfiguration configuration,
            IList<ParameterDetail> consolidatedParametersData)
        {
            // Read parameter values to be assigned from configuration
            foreach (string key in configuration.Options.ParameterConfigurationValues.Keys)
            {
                // check if it's a GUID
                Guid parameterId;
                if (Guid.TryParse(key, out parameterId))
                {
                    var parameterConfigurationValue = configuration.Options.ParameterConfigurationValues[key];
                    var parameterDetail = consolidatedParametersData.FirstOrDefault(d => d.Id == parameterId);

                    if (parameterDetail != null)
                    {
                        parameterDetail.IsInConfiguration = true;
                        parameterDetail.ConfigurationValue = parameterConfigurationValue.Value;
                        parameterDetail.ConfigurationName = configuration.Name;
                    }
                    else
                    {
                        consolidatedParametersData.Add(new ParameterDetail()
                        {
                            FullName = parameterConfigurationValue.Name,
                            ConfigurationValue = parameterConfigurationValue.Value,
                            Id = parameterId,
                            IsInConfiguration = true,
                            ConfigurationName = configuration.Name
                        });
                    }
                }
            }

            // Read parameter values to be assigned from user configuration
            // values in dtproj.user are encrypted using user ke so there is no way to decrypt it on a build server ==> Value = null
            foreach (string key in configuration.Options.ParameterConfigurationSensitiveValues.Keys)
            {
                // check if it's a GUID
                Guid parameterId;
                if (Guid.TryParse(key, out parameterId))
                {
                    var parameterConfigurationValue = configuration.Options.ParameterConfigurationSensitiveValues[key];
                    var parameterDetail = consolidatedParametersData.FirstOrDefault(d => d.Id == parameterId);

                    if (parameterDetail != null)
                    {
                        parameterDetail.IsInConfiguration = true;
                        parameterDetail.IsSensitive = true;
                    }
                    else
                    {
                        consolidatedParametersData.Add(new ParameterDetail()
                        {
                            FullName = parameterConfigurationValue.Name,
                            IsSensitive = true,
                            Id = parameterId,
                            IsInConfiguration = true
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Updates Consolidated parameters with data from extracted project parameters
        /// </summary>
        /// <param name="outputProjectParameters"></param>
        /// <param name="consolidatedParametersData"></param>
        private static void UpdateConsolidatedParametersFromProjectParameters(Parameters outputProjectParameters,
            IList<ParameterDetail> consolidatedParametersData)
        {
            var parameterId = Guid.Empty;

            foreach (Parameter parameter in outputProjectParameters)
            {
                var detailToUpdate =
                    consolidatedParametersData.FirstOrDefault(
                        d =>
                            d.FullName == $"Project::{parameter.Name}" &&
                            Guid.TryParse(parameter.ID, out parameterId));

                if (detailToUpdate != null)
                {
                    detailToUpdate.Parameter = parameter;
                    detailToUpdate.Id = parameterId;
                    detailToUpdate.IsSensitive = parameter.Sensitive;
                    detailToUpdate.OriginalValue = parameter.Value;
                }
                else
                {
                    consolidatedParametersData.Add(new ParameterDetail()
                    {
                        Parameter = parameter,
                        FullName = parameter.Name,
                        Id = parameterId,
                        IsSensitive = parameter.Sensitive,
                        OriginalValue = parameter.Value
                    });
                }
            }
        }

        /// <summary>
        /// Consolidates regular build parameter arguments and sensitive build parameers
        /// </summary>
        /// <param name="parameters">Regular build parameters</param>
        /// <param name="sensitiveParameters">Sensitive build parameters</param>
        /// <returns></returns>
        private IList<ParameterDetail> ConsolidateBuildParameters(IDictionary<string, string> parameters,
            IDictionary<string, string> sensitiveParameters)
        {
            // Check that there is no overlap
            if (parameters != null && sensitiveParameters != null)
            {
                var overlappedParameters =
                    sensitiveParameters.Keys.Intersect(parameters.Keys);


                var overlappedParametersArray = overlappedParameters as string[] ?? overlappedParameters.ToArray();

                if (overlappedParametersArray.Length > 0)
                    throw new Exception(
                        $"Duplicate parameters specified: {string.Join(", ", overlappedParametersArray)}");
            }

            var combinedParameters = new List<ParameterDetail>();

            if (parameters != null)
            {
                combinedParameters.AddRange(parameters.Select(p => new ParameterDetail()
                {
                    FullName = p.Key,
                    BuildParameterValue = p.Value,
                    ForceSensitive = false,
                    IsInBuldParameters = true
                }));
            }

            // join sensitive parameters
            if (sensitiveParameters != null)
            {
                combinedParameters.AddRange(sensitiveParameters.Select(p => new ParameterDetail()
                {
                    FullName = p.Key,
                    BuildParameterValue = p.Value,
                    ForceSensitive = true,
                    IsInBuldParameters = true
                }));
            }

            return combinedParameters;
        }

        /// <summary>
        /// Logs parameters passed to build
        /// </summary>
        /// <param name="projectFilePath"></param>
        /// <param name="protectionLevel"></param>
        /// <param name="password"></param>
        /// <param name="newPassword"></param>
        /// <param name="outputDirectory"></param>
        /// <param name="configurationName"></param>
        /// <param name="parameters"></param>
        /// <param name="sensitiveParameters"></param>
        private void LogParametersUsed(string projectFilePath, string protectionLevel, string password,
            string newPassword,
            string outputDirectory, string configurationName, IDictionary<string, string> parameters,
            IDictionary<string, string> sensitiveParameters)
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
                    $"  {parameter.Key} (Sensitive = false): {parameter.Value}");
            }
            foreach (var sensitiveParameter in sensitiveParameters)
            {
                _logger.LogMessage(
                    $"  {sensitiveParameter.Key} (Sensitive = true): {sensitiveParameter.Value}");
            }
        }

        /// <summary>
        /// Converts a string value to a requested type
        /// </summary>
        /// <param name="value">string value to convert</param>
        /// <param name="type">type to convert to</param>
        /// <returns></returns>
        private static object ConvertToObject(string value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFromString(value);
        }

        /// <summary>
        /// Password protected levels
        /// </summary>
        private static readonly DTSProtectionLevel[] PasswordProtectionLevels =
        {
            DTSProtectionLevel.EncryptAllWithPassword, DTSProtectionLevel.EncryptSensitiveWithPassword
        };

        /// <summary>
        /// Detects whether a specific DTS Protection level is password protected
        /// </summary>
        /// <param name="protectionLevel">DTS Protection Level</param>
        /// <returns></returns>
        private static bool IsPasswordProtectedLevel(DTSProtectionLevel protectionLevel)
        {
            return PasswordProtectionLevels.Contains(protectionLevel);
        }

        /// <summary>
        /// Loads package from a dtsx file.
        /// </summary>
        /// <param name="packagePath">path to a dtsx file</param>
        /// <param name="decryptionPassword"></param>
        /// <returns></returns>
        private static Package LoadPackage(string packagePath, string decryptionPassword)
        {
            // todo: check how to implement security here since the encryption is stored differently on a package level 
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
                if (!string.IsNullOrWhiteSpace(decryptionPassword))
                    package.PackagePassword = decryptionPassword;
                var passwordEventListener = new PasswordEventListener();
                package.LoadFromXML(xml, passwordEventListener);
                if (passwordEventListener.NeedPassword)
                {
                    if (string.IsNullOrWhiteSpace(decryptionPassword))
                    {
                        throw new Exception("Decryption password is required");
                    }
                    throw new Exception("Package password is different from Project password");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error while loading package {packagePath} : {e.Message}.");
            }

            return package;
        }

        /// <summary>
        /// Assigns parameter (project/package) values based on whether the value was passed as a Build Parameter Argument, 
        /// or exists in an active Configuration, or, keeps an original value
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        /// <param name="consolidatedParametersData">Consolidated parameters data</param>
        /// <param name="parameterScope">Either "Project" for project parameters, or package name for package parameters</param>
        private void SetParameterFinalValues(Parameters parameters,
            IList<ParameterDetail> consolidatedParametersData,
            string parameterScope)
        {
            foreach (Parameter parameter in parameters)
            {
                Guid parameterId;
                if (Guid.TryParse(parameter.ID, out parameterId))
                {
                    var parameterDetail = consolidatedParametersData.FirstOrDefault(d => d.FullName == $"{parameterScope}::{parameter.Name}");
                    if (parameterDetail != null)
                    {
                        // Build Parameter Argument wins.
                        if (parameterDetail.IsInBuldParameters)
                        {
                            parameter.Value = ConvertToObject(parameterDetail.BuildParameterValue,
                                parameter.Value.GetType());
                            _logger.LogMessage(
                                $"Using Build Parameter Argument to set {parameterDetail.FullName} value to {parameterDetail.BuildParameterValue}.");
                        }
                        else if (parameterDetail.IsInConfiguration)
                        {
                            if (parameterDetail.IsSensitive && parameterDetail.ConfigurationValue == null)
                            {
                                throw new Exception(
                                    $"Sensitive parameter {parameterDetail.FullName} configuration value is encrypted by the user key and is not decryptable. Either pass the value via a Build Parameter Argument of remove it from configurations.");
                            }
                            parameter.Value = parameterDetail.ConfigurationValue;
                            _logger.LogMessage(
                                $"Using Configuration {parameterDetail.ConfigurationName} to set {parameterDetail.FullName} value to {parameterDetail.ConfigurationValue}.");
                        }
                        else
                        {
                            _logger.LogMessage(
                                $"Using original {parameterScope}::{parameter.Name} value {parameter.Value}.");
                        }
                    }
                    else
                    {
                        _logger.LogMessage(
                            $"Using original {parameterScope}::{parameter.Name} value {parameter.Value}.");
                    }
                }
            }
        }

        /// <summary>
        /// Deserializes source project from a dtproj file
        /// </summary>
        /// <param name="sourceProjectFilePath"></param>
        /// <returns></returns>
        private static ProjectSerialization DeserializeDtproj(string sourceProjectFilePath)
        {
            var xmlOverrides = new XmlAttributeOverrides();
            ProjectConfigurationOptions.PrepareSerializationOverrides(
                typeof(DataTransformationsProjectConfigurationOptions), SerializationLevel.Project, xmlOverrides);

            // Read project file
            var serializer = new XmlSerializer(typeof(ProjectSerialization), xmlOverrides);

            var fileStream = File.OpenRead(sourceProjectFilePath);
            return serializer.Deserialize(fileStream) as ProjectSerialization;
        }

        /// <summary>
        /// Reads the dtproj.user file to find sensitive parameter that are included in configurations and merges them 
        /// with the active configuration options
        /// </summary>
        /// <param name="projectFilePath"></param>
        /// <param name="activeConfiguration"></param>
        /// <returns></returns>
        private static void MergeUserOptions(string projectFilePath,
            DataTransformationsConfiguration activeConfiguration)
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
        /// Reads Project Manifest from deserialized source project
        /// </summary>
        /// <param name="sourceProject">Deserialized source project</param>
        /// <param name="decryptionPassword">Password to decrypt encrypted values</param>
        /// <returns></returns>
        private static ProjectManifest ExtractProjectManifest(ProjectSerialization sourceProject,
            string decryptionPassword)
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

        /// <summary>
        /// Extracts Configuration from deserialized source project
        /// </summary>
        /// <param name="sourceProject">Deserialized source project</param>
        /// <param name="configurationName">Name of configuration to extract</param>
        /// <returns>Project Configuration</returns>
        private static DataTransformationsConfiguration GetProjectConfiguration(ProjectSerialization sourceProject,
            string configurationName)
        {
            foreach (var configurationObject in sourceProject.Configurations)
            {
                var configuration = configurationObject as DataTransformationsConfiguration;
                if (configuration != null &&
                    configuration.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase))
                {
                    return configuration;
                }
            }

            throw new Exception($"Configuration {configurationName} does not exist in the project");
        }

        /// <summary>
        /// Assigns output project properties from source project manifest
        /// </summary>
        /// <param name="outputProject">Output project</param>
        /// <param name="manifestProperties">Properties dictionary from source project manifest</param>
        private static void SetProjectProperties(Project outputProject, IDictionary<string, string> manifestProperties)
        {
            // set the properties we care about
            foreach (var property in manifestProperties.Keys)
            {
                switch (property)
                {
                    case "Name":
                        outputProject.Name = manifestProperties[property];
                        break;
                    case "VersionMajor":
                        outputProject.VersionMajor = int.Parse(manifestProperties[property]);
                        break;
                    case "VersionMinor":
                        outputProject.VersionMinor = int.Parse(manifestProperties[property]);
                        break;
                    case "VersionBuild":
                        outputProject.VersionBuild = int.Parse(manifestProperties[property]);
                        break;
                    case "VersionComments":
                        outputProject.VersionComments = manifestProperties[property];
                        break;
                }
            }
        }
    }
}
