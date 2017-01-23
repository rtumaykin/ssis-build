using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Xml;
using SsisBuild.Helpers;

namespace SsisBuild.Models
{
    public class Project
    {
        public ProtectionLevel ProtectionLevel => (_projectManifest as ProjectManifest)?.ProtectonLevel ?? ProtectionLevel.DontSaveSensitive;

        protected readonly IDictionary<string, Parameter> _parameters;
        public IReadOnlyDictionary<string, Parameter> Parameters { get; }

        private ProjectFile _projectManifest;
        private ProjectFile _projectParameters;
        private IDictionary<string, ProjectFile> _projectConnections;
        private IDictionary<string, ProjectFile> _packages;

        private Project()
        {
            _parameters = new Dictionary<string, Parameter>();

            Parameters = new ReadOnlyDictionary<string, Parameter>(_parameters);

            _packages = new Dictionary<string, ProjectFile>();
            _projectConnections = new Dictionary<string, ProjectFile>();
        }

        public void UpdateParameter(string parameterName, string value, ParameterSource source)
        {
            if (_parameters.ContainsKey(parameterName))
                _parameters[parameterName].SetValue(value, source);
        }

        internal void AddParameter(Parameter parameter)
        {
            _parameters.Add(parameter.Name, parameter);
        }

        public static Project LoadFromIspac(string filePath, string password)
        {
            if (!File.Exists(filePath))
                throw new Exception($"File {filePath} does not exist.");

            if (!filePath.EndsWith(".ispac", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"File {filePath} does not have an .ispac extension.");

            throw new NotImplementedException();
        }

        public static Project LoadFromDtproj(string filePath, string configurationName, string password)
        {
            if (!File.Exists(filePath))
                throw new Exception($"File {filePath} does not exist.");

            if (!filePath.EndsWith(".dtproj", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"File {filePath} does not have an .dtproj extension.");

            var dtprojXmlDoc = new XmlDocument();
            dtprojXmlDoc.Load(filePath);
            ValidateDeploymentMode(dtprojXmlDoc);

            var nsManager = dtprojXmlDoc.GetNameSpaceManager();

            var projectXmlNode = dtprojXmlDoc.SelectSingleNode("/Project/DeploymentModelSpecificContent/Manifest/SSIS:Project", nsManager);

            if (projectXmlNode == null)
                throw new Exception("Project Manifest Node was not found.");

            var projectDirectory = Path.GetDirectoryName(filePath);

            if (string.IsNullOrWhiteSpace(projectDirectory))
                throw new Exception("Failed to retrieve directory of the source project.");

            var project = new Project();

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(projectXmlNode.OuterXml);
                    writer.Flush();
                    stream.Position = 0;

                    project._projectManifest = new ProjectManifest().Initialize(stream, password);
                }
            }

            project._projectParameters = new ProjectParams().Initialize(Path.Combine(projectDirectory, "Project.params"), password);

            foreach (var connectionManagerName in (project._projectManifest as ProjectManifest).ConnectionManagerNames)
            {
                project._projectConnections.Add(connectionManagerName, new ProjectConnection().Initialize(Path.Combine(projectDirectory, connectionManagerName), password));
            }

            foreach (var packageName in (project._projectManifest as ProjectManifest).PackageNames)
            {
                project._packages.Add(packageName, new Package().Initialize(Path.Combine(projectDirectory, packageName), password));
            }

            project.LoadParameters();

            foreach (var configurationParameter in new Configuration(configurationName).Initialize(filePath, password).Parameters)
            {
                project.UpdateParameter(configurationParameter.Key, configurationParameter.Value.Value, ParameterSource.Configuration);
            }

            var userConfigurationFilePath = $"{filePath}.user";
            if (File.Exists(userConfigurationFilePath))
            {
                foreach (var userConfigurationParameter in new UserConfiguration(configurationName).Initialize(userConfigurationFilePath, password).Parameters)
                {
                    project.UpdateParameter(userConfigurationParameter.Key, null, ParameterSource.Configuration);
                }
            }


            return project;
        }
        private static void ValidateDeploymentMode(XmlNode dtprojXmlDoc)
        {
            var deploymentModel = dtprojXmlDoc.SelectSingleNode("/Project/DeploymentModel")?.InnerText;

            if (deploymentModel != "Project")
                throw new Exception("This build method only apply to Project deployment model.");
        }

        private void LoadParameters()
        {
            foreach (var projectParametersParameter in _projectParameters.Parameters)
            {
                _parameters.Add(projectParametersParameter.Key, projectParametersParameter.Value);
            }

            foreach (var projectManifestParameter in _projectManifest.Parameters)
            {
                _parameters.Add(projectManifestParameter.Key, projectManifestParameter.Value);
            }

        }

        public void Save(string destinationFilePath, ProtectionLevel protectionLevel, string password)
        {
            if (Path.GetExtension(destinationFilePath) != ".ispac")
                throw new Exception($"Destination file name must have an ispac extension. Currently: {destinationFilePath}");

            if (File.Exists(destinationFilePath))
                File.Delete(destinationFilePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));

            using (var ispacStream = new FileStream(destinationFilePath, FileMode.Create))
            {
                using (var ispacArchive = new ZipArchive(ispacStream, ZipArchiveMode.Create))
                {
                    var manifest = ispacArchive.CreateEntry("@Project.manifest");
                    using (var stream = manifest.Open())
                    {
                        _projectManifest.Save(stream, protectionLevel, password);
                    }

                    var projectParams = ispacArchive.CreateEntry("Project.params");
                    using (var stream = projectParams.Open())
                    {
                        _projectParameters.Save(stream, protectionLevel, password);
                    }

                    var contentTypes = ispacArchive.CreateEntry("[Content_Types].xml");
                    using (var stream = contentTypes.Open())
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.WriteLine(
                                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"dtsx\" ContentType=\"text/xml\" /><Default Extension=\"conmgr\" ContentType=\"text/xml\" /><Default Extension=\"params\" ContentType=\"text/xml\" /><Default Extension=\"manifest\" ContentType=\"text/xml\" /></Types>");
                        }
                    }

                    foreach (var package in _packages)
                    {
                        var name = PackUriHelper.CreatePartUri(new Uri(package.Key, UriKind.Relative)).OriginalString.Substring(1);
                        var packageEntry = ispacArchive.CreateEntry(name);
                        using (var stream = packageEntry.Open())
                        {
                            package.Value.Save(stream, protectionLevel, password);
                        }
                    }

                    foreach (var projectConnection in _projectConnections)
                    {
                        var name = PackUriHelper.CreatePartUri(new Uri(projectConnection.Key, UriKind.Relative)).OriginalString.Substring(1);
                        var projectConnectionEntry = ispacArchive.CreateEntry(name);
                        using (var stream = projectConnectionEntry.Open())
                        {
                            projectConnection.Value.Save(stream, protectionLevel, password);
                        }
                    }
                }
            }

        }

        public void Save(string destinationFilePath) => Save(destinationFilePath, ProtectionLevel.DontSaveSensitive, null);
    }
}
