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

namespace SsisBuild.Core.ProjectManagement
{
    public class ProjectManifest : ProjectFile, IProjectManifest
    {
        public override ProtectionLevel ProtectionLevel
        {
            get
            {
                var protectionLevelString = _protectionLevelNodes.FirstOrDefault(a => a.NodeType == XmlNodeType.Attribute)?.Value;
                return string.IsNullOrWhiteSpace(protectionLevelString) ? ProtectionLevel.DontSaveSensitive : (ProtectionLevel) Enum.Parse(typeof(ProtectionLevel), protectionLevelString); 
            }
            set
            {
                foreach (var protectionLevelNode in _protectionLevelNodes)
                {
                    if (protectionLevelNode.NodeType == XmlNodeType.Attribute)
                        protectionLevelNode.Value = value.ToString("G");
                    else
                        protectionLevelNode.InnerText = value.ToString("D");
                }
            }
        }

        private readonly IList<XmlElement> _versionMajorNodes;
        private readonly IList<XmlElement> _versionMinorNodes;
        private readonly IList<XmlElement> _versionBuildNodes;
        private readonly IList<XmlElement> _versionCommentsNodes;
        private XmlElement _descriptionNode;
        private readonly IList<XmlNode> _protectionLevelNodes;



        public int VersionMajor
        {
            get { return int.Parse(_versionMajorNodes.FirstOrDefault()?.InnerText??"0"); }
            set
            {
                foreach (var versionMajorNode in _versionMajorNodes)
                {
                    versionMajorNode.InnerText = value.ToString();
                }
            }
        }

        public int VersionMinor
        {
            get { return int.Parse(_versionMinorNodes.FirstOrDefault()?.InnerText??"0"); }
            set
            {
                foreach (var versionMinorNode in _versionMinorNodes)
                {
                    versionMinorNode.InnerText = value.ToString();
                }
            }
        }

        public int VersionBuild
        {
            get { return int.Parse(_versionBuildNodes.FirstOrDefault()?.InnerText??"0"); }
            set
            {
                foreach (var versionBuildNode in _versionBuildNodes)
                {
                    versionBuildNode.InnerText = value.ToString();
                }
            }
        }

        public string VersionComments
        {
            get { return _versionCommentsNodes.FirstOrDefault()?.InnerText; }
            set
            {
                foreach (var versionCommentsNode in _versionCommentsNodes)
                {
                    versionCommentsNode.InnerText = value;
                }
            }
        }

        public string Description
        {
            get { return _descriptionNode.InnerText; }
            set
            {
                _descriptionNode.InnerText = value;
            }
        }

        public ProjectManifest()
        {
            _versionCommentsNodes = new List<XmlElement>();
            _versionBuildNodes = new List<XmlElement>();
            _versionMajorNodes = new List<XmlElement>();
            _protectionLevelNodes = new List<XmlNode>();
            _versionMinorNodes = new List<XmlElement>();
        }

        protected override void PostInitialize()
        {
            var manifestXml = FileXmlDocument.DocumentElement;

            var projectProtectionLevelAttribute = manifestXml?.Attributes["SSIS:ProtectionLevel"];
            var protectionLevelString = projectProtectionLevelAttribute?.Value;

            if (string.IsNullOrWhiteSpace(protectionLevelString))
                throw new InvalidXmlException("Invalid project file. SSIS:Project node must contain a SSIS:ProtectionLevel attribute.", manifestXml);

            ProtectionLevel protectionLevel;
            if (!Enum.TryParse(protectionLevelString, out protectionLevel))
                throw new InvalidXmlException($"Invalid Protection Level {protectionLevelString}.", manifestXml);

            // EncryptAllWithUserKey cannot be decrypted. However, in case of EncryptSensitiveWithUserKey we just loose the sensitive information.
            if (protectionLevel == ProtectionLevel.EncryptAllWithUserKey)
                throw new InvalidProtectionLevelException(protectionLevel);

            _protectionLevelNodes.Add(projectProtectionLevelAttribute);

            PackageNames = ExtractPackageNames();
            ConnectionManagerNames = ExtractConnectionManagerNames();

            foreach (XmlElement packageProtectionLevelNode in FileXmlDocument.SelectNodes("//SSIS:Property[@SSIS:Name = \"ProtectionLevel\"]", NamespaceManager))
            {
                packageProtectionLevelNode.InnerText = protectionLevel.ToString("D");
                _protectionLevelNodes.Add(packageProtectionLevelNode);
            }

            var versionMajorNode = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMajor\"]", NamespaceManager);
            if (versionMajorNode == null)
                throw new InvalidXmlException("Version Major Xml Node was not found", FileXmlDocument);

            var versionMajorString = versionMajorNode.InnerText;

            int test;
            if (!int.TryParse(versionMajorString, out test))
                throw new InvalidXmlException($"Invalid value of Version Major Xml Node: {versionMajorString}.", FileXmlDocument);

            var versionMajorNodes = FileXmlDocument.SelectNodes("//*[@SSIS:Name = \"VersionMajor\"]", NamespaceManager);
            if (versionMajorNodes != null)
            {
                foreach (XmlElement element in versionMajorNodes)
                {
                    if (element != null)
                    {
                        element.InnerText = versionMajorString;
                        _versionMajorNodes.Add(element);
                    }
                }
            }

            var versionMinorNode = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMinor\"]", NamespaceManager);
            if (versionMinorNode == null)
                throw new InvalidXmlException("Version Minor Xml Node was not found", FileXmlDocument);

            var versionMinorString = versionMinorNode.InnerText;

            if (!int.TryParse(versionMinorString, out test))
                throw new InvalidXmlException($"Invalid value of Version Minor Xml Node: {versionMinorString}.", FileXmlDocument);

            var versionMinorNodes = FileXmlDocument.SelectNodes("//*[@SSIS:Name = \"VersionMinor\"]", NamespaceManager);
            if (versionMinorNodes != null)
            {
                foreach (XmlElement element in versionMinorNodes)
                {
                    if (element != null)
                    {
                        element.InnerText = versionMinorString;
                        _versionMinorNodes.Add(element);
                    }
                }
            }
            
            var versionBuildNode = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionBuild\"]", NamespaceManager);
            if (versionBuildNode == null)
                throw new InvalidXmlException("Version Build Xml Node was not found", FileXmlDocument);

            var versionBuildString = versionBuildNode.InnerText;

            if (!int.TryParse(versionBuildString, out test))
                throw new InvalidXmlException($"Invalid value of Version Build Xml Node: {versionBuildString}.", FileXmlDocument);

            var versionBuildNodes = FileXmlDocument.SelectNodes("//*[@SSIS:Name = \"VersionBuild\"]", NamespaceManager);
            if (versionBuildNodes != null)
            {
                foreach (XmlElement element in versionBuildNodes)
                {
                    if (element != null)
                    {
                        element.InnerText = versionBuildString;
                        _versionBuildNodes.Add(element);
                    }
                }
            }

            var versionCommentsNode = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionComments\"]", NamespaceManager);
            if (versionCommentsNode == null)
                throw new InvalidXmlException("Version Comments Xml Node was not found", FileXmlDocument);

            var versionCommentsString = versionCommentsNode.InnerText;

            var versionCommentsNodes = FileXmlDocument.SelectNodes("//*[@SSIS:Name = \"VersionComments\"]", NamespaceManager);
            if (versionCommentsNodes != null)
            {
                foreach (XmlElement element in versionCommentsNodes)
                {
                    if (element != null)
                    {
                        element.InnerText = versionCommentsString;
                        _versionCommentsNodes.Add(element);
                    }
                }
            }

            _descriptionNode = FileXmlDocument.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"Description\"]", NamespaceManager) as XmlElement;
            if (_descriptionNode == null)
                throw new InvalidXmlException("Description Xml Node was not found", FileXmlDocument);
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
