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
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Xml;
using SsisBuild.Core.ProjectManagement.Helpers;

namespace SsisBuild.Core.ProjectManagement
{
    public sealed class Project : IProject
    {
        public ProtectionLevel ProtectionLevel => _isLoaded ? _projectManifest.ProtectionLevel : ProtectionLevel.DontSaveSensitive;

        public int VersionMajor
        {
            get { return _projectManifest?.VersionMajor??0; }
            set
            {
                if (_projectManifest != null)
                    _projectManifest.VersionMajor = value;
            }
        }

        public int VersionMinor
        {
            get { return _projectManifest?.VersionMinor??0; }
            set
            {
                if (_projectManifest != null)
                    _projectManifest.VersionMinor = value;
            }
        }

        public int VersionBuild
        {
            get { return _projectManifest?.VersionBuild??0; }
            set
            {
                if (_projectManifest != null)
                    _projectManifest.VersionBuild = value;
            }
        }

        public string VersionComments
        {
            get { return _projectManifest?.VersionComments; }
            set
            {
                if (_projectManifest != null)
                    _projectManifest.VersionComments = value;
            }
        }

        public string Description
        {
            get { return _projectManifest?.Description; }
            set
            {
                if (_projectManifest != null)
                    _projectManifest.Description = value;
            }
        }
        private IDictionary<string, IParameter> _parameters;

        private IReadOnlyDictionary<string, IParameter> _readonlyParameters;

        public IReadOnlyDictionary<string, IParameter> Parameters
        {
            get
            {
                if (_isLoaded && _parameters == null)
                {
                    _parameters = new Dictionary<string, IParameter>();

                    if (_projectParams != null)
                        foreach (var projectParametersParameter in _projectParams.Parameters)
                        {
                            _parameters.Add(projectParametersParameter.Key, projectParametersParameter.Value);
                        }

                    if (_projectManifest != null)
                        foreach (var projectManifestParameter in _projectManifest.Parameters)
                        {
                            _parameters.Add(projectManifestParameter.Key, projectManifestParameter.Value);
                        }

                    _readonlyParameters = new ReadOnlyDictionary<string, IParameter>(_parameters);
                }

                return _readonlyParameters;
            }
        }

        private IProjectManifest _projectManifest;
        private IProjectFile _projectParams;
        private readonly IDictionary<string, IProjectFile> _projectConnections;
        private readonly IDictionary<string, IProjectFile> _packages;

        private bool _isLoaded;

        public Project()
        {


            _packages = new Dictionary<string, IProjectFile>();
            _projectConnections = new Dictionary<string, IProjectFile>();

            _isLoaded = false;
        }

        public void UpdateParameter(string parameterName, string value, ParameterSource source)
        {
            if (!_isLoaded)
                throw new ProjectNotInitializedException();

            if (Parameters.ContainsKey(parameterName))
                Parameters[parameterName].SetValue(value, source);
        }

        public void Save(string destinationFilePath, ProtectionLevel protectionLevel, string password)
        {
            if (!_isLoaded)
                throw new ProjectNotInitializedException();

            if (Path.GetExtension(destinationFilePath) != ".ispac")
                throw new InvalidExtensionException(destinationFilePath, "ispac");

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));

            using (var ispacStream = new FileStream(destinationFilePath, FileMode.Create))
            {
                Save(ispacStream, protectionLevel, password);
            }

        }

        public void Save(string destinationFilePath) => Save(destinationFilePath, ProtectionLevel.DontSaveSensitive, null);

        public void Save(Stream destinationStream, ProtectionLevel protectionLevel, string password)
        {
            if (!_isLoaded)
                throw new ProjectNotInitializedException();

            using (var ispacArchive = new ZipArchive(destinationStream, ZipArchiveMode.Create))
            {
                var manifest = ispacArchive.CreateEntry("@Project.manifest");
                using (var stream = manifest.Open())
                {
                    _projectManifest.Save(stream, protectionLevel, password);
                }

                var projectParams = ispacArchive.CreateEntry("Project.params");
                using (var stream = projectParams.Open())
                {
                    _projectParams.Save(stream, protectionLevel, password);
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
        public void LoadFromIspac(string filePath, string password)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} does not exist or you don't have permissions to access it.", filePath);

            if (!filePath.EndsWith(".ispac", StringComparison.OrdinalIgnoreCase))
                throw new InvalidExtensionException(filePath, "ispac");

            using (var ispacStream = new FileStream(filePath, FileMode.Open))
            {
                using (var ispacArchive = new ZipArchive(ispacStream, ZipArchiveMode.Read))
                {
                    foreach (var ispacArchiveEntry in ispacArchive.Entries)
                    {
                        var fileName = ispacArchiveEntry.FullName;
                        using (var fileStream = ispacArchiveEntry.Open())
                        {
                            switch (Path.GetExtension(fileName))
                            {
                                case ".manifest":
                                    _projectManifest = new ProjectManifest();
                                    _projectManifest.Initialize(fileStream, password);
                                    break;

                                case ".params":
                                    _projectParams = new ProjectParams();
                                    _projectParams.Initialize(fileStream, password);
                                    break;

                                case ".dtsx":
                                    var package = new Package();
                                    package.Initialize(fileStream, password);
                                    _packages.Add(fileName, package);
                                    break;

                                case ".conmgr":
                                    var projectConnection = new ProjectConnection();
                                    projectConnection.Initialize(fileStream, password);
                                    _projectConnections.Add(fileName, projectConnection);
                                    break;

                                case ".xml":
                                    break;
                            }
                        }
                    }
                }
            }

            _isLoaded = true;
        }

        public void LoadFromDtproj(string filePath, string configurationName, string password)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} does not exist.", filePath);

            if (!filePath.EndsWith(".dtproj", StringComparison.OrdinalIgnoreCase))
                throw new InvalidExtensionException(filePath, "dtproj");

            var dtprojXmlDoc = new XmlDocument();
            dtprojXmlDoc.Load(filePath);
            ValidateDeploymentMode(dtprojXmlDoc);

            var nsManager = dtprojXmlDoc.GetNameSpaceManager();

            var projectXmlNode = dtprojXmlDoc.SelectSingleNode("/Project/DeploymentModelSpecificContent/Manifest/SSIS:Project", nsManager);

            if (projectXmlNode == null)
                throw new InvalidXmlException("Project Manifest Node was not found.", dtprojXmlDoc);

            var projectDirectory = Path.GetDirectoryName(filePath);

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(projectXmlNode.OuterXml);
                    writer.Flush();
                    stream.Position = 0;

                    _projectManifest = new ProjectManifest();
                    _projectManifest.Initialize(stream, password);
                }
            }

            _projectParams = new ProjectParams();
            _projectParams.Initialize(Path.Combine(projectDirectory, "Project.params"), password);

            foreach (var connectionManagerName in _projectManifest.ConnectionManagerNames)
            {
                var projectConnection = new ProjectConnection();
                projectConnection.Initialize(Path.Combine(projectDirectory, connectionManagerName), password);
                _projectConnections.Add(connectionManagerName, projectConnection);
            }

            foreach (var packageName in _projectManifest.PackageNames)
            {
                var package = new Package();
                package.Initialize(Path.Combine(projectDirectory, packageName), password);
                _packages.Add(packageName, package);
            }

            _isLoaded = true;

            var configuration = new Configuration(configurationName);
            configuration.Initialize(filePath, password);
            foreach (var configurationParameter in configuration.Parameters)
            {
                UpdateParameter(configurationParameter.Key, configurationParameter.Value.Value, ParameterSource.Configuration);
            }

            var userConfigurationFilePath = $"{filePath}.user";

            if (File.Exists(userConfigurationFilePath))
            {
                var userConfiguration = new UserConfiguration(configurationName);
                userConfiguration.Initialize(userConfigurationFilePath, password);
                foreach (var userConfigurationParameter in userConfiguration.Parameters)
                {
                    UpdateParameter(userConfigurationParameter.Key, null, ParameterSource.Configuration);
                }
            }
        }

        private static void ValidateDeploymentMode(XmlNode dtprojXmlDoc)
        {
            var deploymentModel = dtprojXmlDoc.SelectSingleNode("/Project/DeploymentModel")?.InnerText;

            if (deploymentModel != "Project")
                throw new InvalidDeploymentModelException();
        }
    }
}
