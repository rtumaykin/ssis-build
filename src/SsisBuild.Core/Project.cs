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
using SsisBuild.Core.Helpers;

namespace SsisBuild.Core
{
    public class Project
    {
        public ProtectionLevel ProtectionLevel => ProjectManifest?.ProtectonLevel ?? ProtectionLevel.DontSaveSensitive;

        public string VersionMajor
        {
            get { return ProjectManifest?.VersionMajor; }
            set
            {
                if (ProjectManifest != null)
                    ProjectManifest.VersionMajor = value;
            }
        }

        public string VersionMinor
        {
            get { return ProjectManifest?.VersionMinor; }
            set
            {
                if (ProjectManifest != null)
                    ProjectManifest.VersionMinor = value;
            }
        }

        public string VersionBuild
        {
            get { return ProjectManifest?.VersionBuild; }
            set
            {
                if (ProjectManifest != null)
                    ProjectManifest.VersionBuild = value;
            }
        }

        public string VersionComments
        {
            get { return ProjectManifest?.VersionComments; }
            set
            {
                if (ProjectManifest != null)
                    ProjectManifest.VersionComments = value;
            }
        }

        public string Description
        {
            get { return ProjectManifest?.Description; }
            set
            {
                if (ProjectManifest != null)
                    ProjectManifest.Description = value;
            }
        }
        internal ProjectManifest ProjectManifest => (ManifestProjectFile as ProjectManifest);

        protected readonly IDictionary<string, Parameter> _parameters;
        public IReadOnlyDictionary<string, Parameter> Parameters { get; }

        internal ProjectFile ManifestProjectFile;
        internal ProjectFile ParametersProjectFile;
        internal readonly IDictionary<string, ProjectFile> ConnectionsProjectFiles;
        internal readonly IDictionary<string, ProjectFile> PackagesProjectFiles;

        internal Project()
        {
            _parameters = new Dictionary<string, Parameter>();

            Parameters = new ReadOnlyDictionary<string, Parameter>(_parameters);

            PackagesProjectFiles = new Dictionary<string, ProjectFile>();
            ConnectionsProjectFiles = new Dictionary<string, ProjectFile>();
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

        internal void LoadParameters()
        {
            foreach (var projectParametersParameter in ParametersProjectFile.Parameters)
            {
                _parameters.Add(projectParametersParameter.Key, projectParametersParameter.Value);
            }

            foreach (var projectManifestParameter in ManifestProjectFile.Parameters)
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
                Save(ispacStream, protectionLevel, password);
            }

        }

        public void Save(string destinationFilePath) => Save(destinationFilePath, ProtectionLevel.DontSaveSensitive, null);

        public void Save(Stream destinationStream, ProtectionLevel protectionLevel, string password)
        {
            using (var ispacArchive = new ZipArchive(destinationStream, ZipArchiveMode.Create))
            {
                var manifest = ispacArchive.CreateEntry("@Project.manifest");
                using (var stream = manifest.Open())
                {
                    ManifestProjectFile.Save(stream, protectionLevel, password);
                }

                var projectParams = ispacArchive.CreateEntry("Project.params");
                using (var stream = projectParams.Open())
                {
                    ParametersProjectFile.Save(stream, protectionLevel, password);
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

                foreach (var package in PackagesProjectFiles)
                {
                    var name = PackUriHelper.CreatePartUri(new Uri(package.Key, UriKind.Relative)).OriginalString.Substring(1);
                    var packageEntry = ispacArchive.CreateEntry(name);
                    using (var stream = packageEntry.Open())
                    {
                        package.Value.Save(stream, protectionLevel, password);
                    }
                }

                foreach (var projectConnection in ConnectionsProjectFiles)
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
}
