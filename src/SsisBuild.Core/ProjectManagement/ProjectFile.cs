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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using SsisBuild.Core.ProjectManagement.Helpers;

namespace SsisBuild.Core.ProjectManagement
{
    public abstract class ProjectFile : IProjectFile
    {
        public virtual ProtectionLevel ProtectionLevel { get; set; }

        protected XmlDocument FileXmlDocument;
        protected XmlNamespaceManager NamespaceManager;

        private readonly IDictionary<string, IParameter> _parameters;
        public IReadOnlyDictionary<string, IParameter> Parameters { get; }

        private bool _isInitialized;

        protected ProjectFile()
        {
            _parameters = new Dictionary<string, IParameter>();

            Parameters = new ReadOnlyDictionary<string, IParameter>(_parameters);

            FileXmlDocument = new XmlDocument();

            _isInitialized = false;
        }

        public void Initialize(string filePath, string password)
        {
            using (var stream = File.OpenRead(filePath))
            {
                Initialize(stream, password);
            }
        }

        public void Initialize(Stream fileStream, string password)
        {
            _isInitialized = true;
            Load(fileStream);
            Decrypt(password);
            var parameters = ExtractParameters();
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    _parameters.Add(parameter.Name, parameter);
                }
            }

            PostInitialize();
        }

        protected virtual void PostInitialize()
        {
        }

        protected virtual IList<IParameter> ExtractParameters()
        {
            return null;
        }

        private void Load(Stream fileStream)
        {
            FileXmlDocument.Load(fileStream);
            NamespaceManager = FileXmlDocument.GetNameSpaceManager();
        }

        public void Save(Stream fileStream)
        {
            Save(fileStream, ProtectionLevel.DontSaveSensitive, null);
        }

        public void Save(Stream fileStream, ProtectionLevel protectionLevel, string password)
        {
            ThrowIfNotInitialized();

            var xmlToSave = PrepareXmlToSave(protectionLevel, password);

            xmlToSave.Save(fileStream);
        }

        public void Save(string filePath)
        {
            Save(filePath, ProtectionLevel.DontSaveSensitive, null);
        }

        public void Save(string filePath, ProtectionLevel protectionLevel, string password)
        {
            ThrowIfNotInitialized();

            var fullPath = Path.GetFullPath(filePath);
            var destinationDirectory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(destinationDirectory);

            var xmlToSave = PrepareXmlToSave(protectionLevel, password);

            xmlToSave.Save(fullPath);
        }

        private void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
                throw new ProjectNotInitializedException();
        }

        private XmlDocument PrepareXmlToSave(ProtectionLevel protectionLevel, string password)
        {
            ProtectionLevel = protectionLevel;

            var xmlToSave = new XmlDocument();
            xmlToSave.LoadXml(FileXmlDocument.OuterXml);

            if (!new[]
            {
                ProtectionLevel.DontSaveSensitive,
                ProtectionLevel.EncryptSensitiveWithUserKey,
                ProtectionLevel.EncryptAllWithPassword,
                ProtectionLevel.EncryptSensitiveWithPassword,
                ProtectionLevel.ServerStorage
            }.Contains(protectionLevel))
                throw new InvalidProtectionLevelException(protectionLevel);

            if (protectionLevel == ProtectionLevel.EncryptAllWithPassword)
            {
                EncryptElement(xmlToSave.DocumentElement, password);
            }
            else
            {
                var sensitiveElements = GetSensitiveElements(xmlToSave);
                foreach (var sensitiveElement in sensitiveElements)
                {
                    if (protectionLevel == ProtectionLevel.EncryptSensitiveWithPassword)
                        EncryptElement(sensitiveElement, password);

                    if (protectionLevel == ProtectionLevel.DontSaveSensitive)
                        sensitiveElement.ParentNode?.RemoveChild(sensitiveElement);
                }
            }
            return xmlToSave;
        }

        private void Decrypt(string password)
        {
            var encryptedElements = GetEncryptedElements(FileXmlDocument);
            foreach (var encryptedElement in encryptedElements)
            {
                if (string.IsNullOrEmpty(password))
                    throw new InvalidPaswordException();

                DecryptElement(encryptedElement, password);
            }
        }
        protected IList<XmlElement> GetEncryptedElements(XmlNode rootNode)
        {
            var encryptedElements = new List<XmlElement>();

            var nodes = rootNode.SelectNodes("//*[@Salt or @SSIS:Salt]",
                rootNode.GetNameSpaceManager());



            if (nodes != null)
            {
                encryptedElements.AddRange(nodes.OfType<XmlElement>());
            }

            return encryptedElements;
        }

        protected virtual void EncryptElement(XmlElement element, string password)
        {
            if (password == null)
                throw new InvalidPaswordException();

            var rgbSalt = new byte[7];
            new RNGCryptoServiceProvider().GetBytes(rgbSalt);
            var cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            var passwordDeriveBytes = new PasswordDeriveBytes(password, rgbSalt);

            var document = element.GetDocument();

            cryptoServiceProvider.Key = passwordDeriveBytes.CryptDeriveKey("TripleDES", "SHA1", 192, cryptoServiceProvider.IV);
            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, cryptoServiceProvider.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (var streamWriter = new StreamWriter(cryptoStream, Encoding.UTF8))
                        streamWriter.Write(element.OuterXml);
                }
                var array = memoryStream.ToArray();
                foreach (XmlNode childNode in element.ChildNodes)
                    element.RemoveChild(childNode);
                element.InnerText = Convert.ToBase64String(array);
            }
            var saltAttribute = document.CreateAttribute("Salt", XmlHelpers.Schemas.SSIS);
            saltAttribute.Value = Convert.ToBase64String(rgbSalt);
            element.SetAttribute("Salt", XmlHelpers.Schemas.SSIS, Convert.ToBase64String(rgbSalt));
            element.SetAttribute("IV", XmlHelpers.Schemas.SSIS, Convert.ToBase64String(cryptoServiceProvider.IV));
        }

        protected IList<XmlElement> GetSensitiveElements(XmlNode rootNode)
        {
            var sensitiveElements = new List<XmlElement>();

            var sensitiveNodesNumberAttributeValue = rootNode.SelectNodes("//*[@Sensitive=\"1\" or @SSIS:Sensitive=\"1\"]", NamespaceManager);

            if (sensitiveNodesNumberAttributeValue != null)
                sensitiveElements.AddRange(sensitiveNodesNumberAttributeValue.OfType<XmlElement>());

            // Package has an old way of dealing with it.
            var sensitiveNodesStringAttributeValue =
                rootNode.SelectNodes(
                    "//DTS:PackageParameter[@DTS:Sensitive=\"True\"]/DTS:Property[@DTS:Name=\"ParameterValue\"]",
                    NamespaceManager);

            if (sensitiveNodesStringAttributeValue != null)
                sensitiveElements.AddRange(sensitiveNodesStringAttributeValue.OfType<XmlElement>());

            return sensitiveElements;
        }

        protected virtual void DecryptElement(XmlElement element, string password)
        {
            var saltXmlAttributeNode = XmlHelpers.GetAttributeNode(element, "Salt");
            if (string.IsNullOrEmpty(saltXmlAttributeNode?.Value))
            {
                throw new InvalidXmlException($"Encrypted element {element.Name} does not contain required Attribute \"Salt\", or its contents is empty", element);
            }
            byte[] rgbSalt;
            try
            {
                rgbSalt = Convert.FromBase64String(saltXmlAttributeNode.Value);
            }
            catch (FormatException)
            {
                throw new InvalidXmlException($"Invalid value of Attribute \"Salt\" ({saltXmlAttributeNode.Value}) in encrypted element {element.Name}", element);
            }
            var ivXmlAttributeNode = XmlHelpers.GetAttributeNode(element, "IV");
            if (string.IsNullOrEmpty(ivXmlAttributeNode?.Value))
            {
                throw new InvalidXmlException($"Encrypted element {element.Name} does not contain required Attribute \"IV\", or its contents is empty", element);
            }
            byte[] iv;
            try
            {
                iv = Convert.FromBase64String(ivXmlAttributeNode.Value);
            }
            catch (FormatException)
            {
                throw new InvalidXmlException($"Invalid value of Attribute \"IV\" ({ivXmlAttributeNode.Value}) in encrypted element {element.Name} ", element);
            }
            var cryptoServiceProvider = new TripleDESCryptoServiceProvider {IV = iv};

            var passwordDeriveBytes = new PasswordDeriveBytes(password, rgbSalt);

            cryptoServiceProvider.Key = passwordDeriveBytes.CryptDeriveKey("TripleDES", "SHA1", 192,
                cryptoServiceProvider.IV);
            string xml;

            byte[] buffer;
            try
            {
                buffer = Convert.FromBase64String(element.InnerText);
            }
            catch (FormatException)
            {
                throw new InvalidXmlException($"Invalid value of encrypted element {element.Name}.", element);
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
            
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            // The reason to not simply import the new node is because namespace declaration will also be imported with the node.
            element.Attributes.Remove(saltXmlAttributeNode);
            element.Attributes.Remove(ivXmlAttributeNode);

            foreach (XmlNode childNode in element.ChildNodes)
                element.RemoveChild(childNode);
            element.InnerXml = xmlDocument.DocumentElement?.InnerXml;
        }
    }
}
