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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Dts.Runtime;

namespace SsisBuild
{
    public static class Decryptor
    {
        private static XmlDocument GetOwnerDocument(XmlNode xmlNode)
            => xmlNode.NodeType == XmlNodeType.Document ? xmlNode as XmlDocument : xmlNode.OwnerDocument;

        private static XmlNamespaceManager GetNameSpaceManager(XmlNode rootNode)
        {
            var namespaceManager = new XmlNamespaceManager(GetOwnerDocument(rootNode).NameTable);
            namespaceManager.AddNamespace("SSIS", "www.microsoft.com/SqlServer/SSIS");
            namespaceManager.AddNamespace("DTS", "www.microsoft.com/SqlServer/Dts");
            return namespaceManager;
        }

        private static XmlNodeList GetSensitiveNodes(XmlNode rootNode)
        {
            return rootNode.SelectNodes("//*[@Sensitive=\"1\" or @SSIS:Sensitive=\"1\"]",
                GetNameSpaceManager(rootNode));
        }

        public static void DecryptXmlNode(XmlNode xmlNode, DTSProtectionLevel protectionLevel, string projectPassword)
        {
            switch (protectionLevel)
            {
                case DTSProtectionLevel.EncryptSensitiveWithPassword:
                    var sensitiveXmlNodes = GetSensitiveNodes(xmlNode);
                    foreach (XmlNode sensitiveXmlNode in sensitiveXmlNodes)
                    {
                        DecryptByPassword(sensitiveXmlNode, projectPassword);
                    }
                    break;

                case DTSProtectionLevel.EncryptAllWithPassword:

                    DecryptByPassword(xmlNode, projectPassword);
                    break;
            }
        }

        private static XmlAttribute GetNamedItem(XmlNode rootNode, string name)
        {
            return rootNode.Attributes?.GetNamedItem(name, "www.microsoft.com/SqlServer/SSIS") as XmlAttribute ??
                   rootNode.Attributes?.GetNamedItem(name) as XmlAttribute;
        }

        private static void DecryptByPassword(XmlNode xmlNode, string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidPaswordException();


            var saltXmlAttribute = GetNamedItem(xmlNode, "Salt");
            if (saltXmlAttribute == null)
            {
                throw new Exception($"Encrypted node {xmlNode.Name} does not contain required Attribute \"Salt\"");
            }
            byte[] rgbSalt;
            try
            {
                rgbSalt = Convert.FromBase64String(saltXmlAttribute.Value);
            }
            catch (FormatException)
            {
                throw new Exception(
                    $"Invalid value of Attribute \"Salt\" ({saltXmlAttribute.Value}) in encrypted node {xmlNode.Name} ");
            }
            var ivXmlAttribute = GetNamedItem(xmlNode, "IV");
            if (ivXmlAttribute == null)
            {
                throw new Exception($"Encrypted node {xmlNode.Name} does not contain required Attribute \"IV\"");
            }
            byte[] numArray;
            try
            {
                numArray = Convert.FromBase64String(ivXmlAttribute.Value);
            }
            catch (FormatException)
            {
                throw new Exception(
                    $"Invalid value of Attribute \"IV\" ({ivXmlAttribute.Value}) in encrypted node {xmlNode.Name} ");
            }
            var cryptoServiceProvider = new TripleDESCryptoServiceProvider {IV = numArray};

            var passwordDeriveBytes = new PasswordDeriveBytes(password, rgbSalt);

            cryptoServiceProvider.Key = passwordDeriveBytes.CryptDeriveKey("TripleDES", "SHA1", 192,
                cryptoServiceProvider.IV);
            string xml;

            byte[] buffer;
            try
            {
                buffer = Convert.FromBase64String(xmlNode.InnerText);
            }
            catch (FormatException)
            {
                throw new Exception(
                    $"Invalid value of encrypted node {xmlNode.Name}: {xmlNode.InnerText.Substring(0, 64)}");
            }
            try
            {
                using (var memoryStream = new MemoryStream(buffer))
                {
                    using (
                        var cryptoStream = new CryptoStream(memoryStream, cryptoServiceProvider.CreateDecryptor(),
                            CryptoStreamMode.Read))
                    {
                        using (var streamReader = new StreamReader(cryptoStream, Encoding.UTF8))
                            xml = streamReader.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException)
            {
                throw new InvalidPaswordException();
            }
            catch (ArgumentException)
            {
                throw new InvalidPaswordException();
            }
            var xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.LoadXml(xml);
            }
            catch (XmlException)
            {
                throw new Exception(
                    $"Invalid decrypted xml in node {xmlNode.Name}: {xml.Substring(0, 64)}");
            }
            foreach (XmlNode childNode in xmlNode.ChildNodes)
                xmlNode.RemoveChild(childNode);
            xmlNode.InnerXml = xmlDocument.DocumentElement?.InnerXml;
        }
    }
}
