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
using System.Linq;
using System.Xml;
using SsisBuild.Core.Helpers;

namespace SsisBuild.Core
{
    public class ProjectManifest : ProjectFile, IProjectManifest
    {
        public ProtectionLevel ProtectonLevel { get; private set; }

        private XmlElement _versionMajor;
        private XmlElement _versionMinor;
        private XmlElement _versionBuild;
        private XmlElement _versionComments;
        private XmlElement _description;



        public string VersionMajor
        {
            get { return _versionMajor?.InnerText; }
            set
            {
                if (_versionMajor != null)
                _versionMajor.InnerText = value;
            }
        }

        public string VersionMinor
        {
            get { return _versionMinor?.InnerText; }
            set
            {
                if (_versionMinor != null)
                    _versionMinor.InnerText = value;
            }
        }

        public string VersionBuild
        {
            get { return _versionBuild?.InnerText; }
            set
            {
                if (_versionBuild != null)
                    _versionBuild.InnerText = value;
            }
        }

        public string VersionComments
        {
            get { return _versionComments?.InnerText; }
            set
            {
                if (_versionComments != null)
                    _versionComments.InnerText = value;
            }
        }

        public string Description
        {
            get { return _description?.InnerText; }
            set
            {
                if (_description != null)
                    _description.InnerText = value;
            }
        }

        protected override void PostInitialize()
        {
            ProtectonLevel = ExtractProtectionLevel();
            PackageNames = ExtractPackageNames();
            ConnectionManagerNames = ExtractConnectionManagerNames();
            _versionMajor = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMajor\"]", NamespaceManager) as XmlElement;
            _versionMinor = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMinor\"]", NamespaceManager) as XmlElement;
            _versionBuild = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionBuild\"]", NamespaceManager) as XmlElement;
            _versionComments = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionComments\"]", NamespaceManager) as XmlElement;
            _description = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"Description\"]", NamespaceManager) as XmlElement;
        }

        private string[] ExtractPackageNames()
        {
            return FileXmlDocument.SelectNodes("/SSIS:Project/SSIS:Packages/SSIS:Package", NamespaceManager)?
                .OfType<XmlElement>().Select(e => e.Attributes["SSIS:Name"]?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();
        }

        private string[] ExtractConnectionManagerNames()
        {
            return FileXmlDocument.SelectNodes("/SSIS:Project/SSIS:ConnectionManagers/SSIS:ConnectionManager", NamespaceManager)?
                .OfType<XmlElement>().Select(e => e.Attributes["SSIS:Name"]?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();
        }

        public string[] PackageNames { get; private set; }

        public string[] ConnectionManagerNames { get; private set; }

        private ProtectionLevel ExtractProtectionLevel()
        {
            var manifestXml = FileXmlDocument.DocumentElement;

            if (manifestXml?.Attributes == null || manifestXml.Attributes.Count == 0 || manifestXml.Attributes["SSIS:ProtectionLevel"] == null)
                throw new Exception("Invalid project file. SSIS:Project node must contain a SSIS:ProtectionLevel attribute.");

            var protectionLevelString = manifestXml.Attributes["SSIS:ProtectionLevel"]?.Value;

            if (string.IsNullOrWhiteSpace(protectionLevelString))
                throw new Exception("Empty SSIS:ProtectionLevel attribute");


            ProtectionLevel protectionLevel;
            if (Enum.TryParse(protectionLevelString, out protectionLevel))
            {
                if (protectionLevel == ProtectionLevel.EncryptAllWithUserKey || protectionLevel == ProtectionLevel.EncryptSensitiveWithUserKey)
                    throw new Exception("Original project can\'t be encrypted with user key since it is not decryptable by a build agent.");

                return protectionLevel;
            }

            throw new Exception($"Invalid Protection Level {protectionLevelString}.");
        }

        protected override void SetProtectionLevel(XmlDocument protectedXmlDocument, ProtectionLevel protectionLevel)
        {
            var manifestXml = protectedXmlDocument.DocumentElement;

            if (manifestXml?.Attributes == null || manifestXml.Attributes.Count == 0 || manifestXml.Attributes["SSIS:ProtectionLevel"] == null)
                throw new Exception("Invalid project file. SSIS:Project node must contain a SSIS:ProtectionLevel attribute.");

            manifestXml.Attributes["SSIS:ProtectionLevel"].Value = protectionLevel.ToString();
        }

        protected override IList<IParameter> ExtractParameters()
        {
            var parameters = new List<IParameter>();

            var projectConnectionParameterXmlNodes = FileXmlDocument.SelectNodes("/SSIS:Project/SSIS:DeploymentInfo/SSIS:ProjectConnectionParameters/SSIS:Parameter",
                NamespaceManager);

            if (projectConnectionParameterXmlNodes != null)
            {
                foreach (XmlNode projectConnectionParameterXmlNode in projectConnectionParameterXmlNodes)
                {
                   parameters.Add(new ProjectParameter("Project", projectConnectionParameterXmlNode));
                }
            }

            var packageParameterXmlNodes = FileXmlDocument.SelectNodes("/SSIS:Project/SSIS:DeploymentInfo/SSIS:PackageInfo/SSIS:PackageMetaData/SSIS:Parameters/SSIS:Parameter",
                NamespaceManager);

            if (packageParameterXmlNodes != null)
            {
                foreach (XmlNode packageParameterXmlNode in packageParameterXmlNodes)
                {
                    var packageName = packageParameterXmlNode.SelectSingleNode("../../SSIS:Properties/SSIS:Property[@SSIS:Name = \"Name\"]", NamespaceManager)?.InnerText;
                    
                    parameters.Add(new ProjectParameter(packageName, packageParameterXmlNode));
                }
            }

            return parameters;
        }
    }
}
