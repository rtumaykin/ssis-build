using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SsisBuild.Helpers;

namespace SsisBuild.Models
{
    public class ProjectManifest : ProjectFile
    {
        public ProtectionLevel ProtectonLevel { get; private set; }

        protected override void PostInitialize()
        {
            ProtectonLevel = ExtractProtectionLevel();
            PackageNames = ExtractPackageNames();
            ConnectionManagerNames = ExtractConnectionManagerNames();
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

            var protectionLevelString = manifestXml.Attributes["SSIS:ProtectionLevel"].Value;

            if (string.IsNullOrWhiteSpace(protectionLevelString))
                throw new Exception("Empty SSIS:ProtectionLevel attribute");


            ProtectionLevel protectionLevel;
            if (Enum.TryParse(manifestXml.Attributes["SSIS:ProtectionLevel"].Value, out protectionLevel))
                return protectionLevel;

            throw new Exception($"Invalid Protection Level {protectionLevelString}.");
        }

        protected override IList<Parameter> ExtractParameters()
        {
            var parameters = new List<Parameter>();

            var projectConnectionParameterXmlNodes = FileXmlDocument.SelectNodes("/SSIS:Project/SSIS:DeploymentInfo/SSIS:ProjectConnectionParameters/SSIS:Parameter",
                NamespaceManager);

            if (projectConnectionParameterXmlNodes != null)
            {
                foreach (XmlNode projectConnectionParameterXmlNode in projectConnectionParameterXmlNodes)
                {
                    var name = $"Project::{projectConnectionParameterXmlNode.GetAttribute("SSIS:Name").Value}";

                    var parentElement = projectConnectionParameterXmlNode.SelectSingleNode("./SSIS:Properties", NamespaceManager) as XmlElement;

                    var valueNode = parentElement?.SelectSingleNode("./SSIS:Property[@SSIS:Name = \"Value\"]", NamespaceManager) as XmlElement;

                    var value = valueNode?.InnerText;

                    parameters.Add(new Parameter(name, value, ParameterSource.Original, valueNode, parentElement));
                }
            }

            var packageParameterXmlNodes = FileXmlDocument.SelectNodes("/SSIS:Project/SSIS:DeploymentInfo/SSIS:PackageInfo/SSIS:PackageMetaData/SSIS:Parameters/SSIS:Parameter",
                NamespaceManager);

            if (packageParameterXmlNodes != null)
            {
                foreach (XmlNode packageParameterXmlNode in packageParameterXmlNodes)
                {
                    var name =
                        $"{packageParameterXmlNode.SelectSingleNode("../../SSIS:Properties/SSIS:Property[@SSIS:Name = \"Name\"]", NamespaceManager)?.InnerText}::{packageParameterXmlNode.GetAttribute("SSIS:Name").Value}";

                    var parentElement = packageParameterXmlNode.SelectSingleNode("./SSIS:Properties", NamespaceManager) as XmlElement;

                    var valueNode = parentElement?.SelectSingleNode("./SSIS:Property[@SSIS:Name = \"Value\"]", NamespaceManager) as XmlElement;

                    var value = valueNode?.InnerText;

                    parameters.Add(new Parameter(name, value, ParameterSource.Original, valueNode, parentElement));
                }
            }

            return parameters;
        }
    }
}
