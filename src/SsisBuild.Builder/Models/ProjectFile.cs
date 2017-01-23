using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using SsisBuild.Helpers;

namespace SsisBuild.Models
{
    public abstract class ProjectFile
    {
        protected XmlDocument FileXmlDocument;
        protected XmlNamespaceManager NamespaceManager;

        private readonly IDictionary<string, Parameter> _parameters;
        public IReadOnlyDictionary<string, Parameter> Parameters { get; }

        private bool _isInitialized;

        protected ProjectFile()
        {
            _parameters = new Dictionary<string, Parameter>();

            Parameters = new ReadOnlyDictionary<string, Parameter>(_parameters);

            FileXmlDocument = new XmlDocument();

            _isInitialized = false;
        }

        public ProjectFile Initialize(string filePath, string password)
        {
            return Initialize(File.OpenRead(filePath), password);
        }

        public ProjectFile Initialize(Stream fileStream, string password)
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

            return this;
        }

        protected virtual void PostInitialize()
        {
        }

        protected virtual IList<Parameter> ExtractParameters()
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
            if (!_isInitialized)
                throw new Exception("You must initialize project file before you can save it.");

            var xmlToSave = PrepareXmlToSave(protectionLevel, password);

            xmlToSave.Save(fileStream);
        }

        public void Save(string filePath)
        {
            Save(filePath, ProtectionLevel.DontSaveSensitive, null);
        }

        public void Save(string filePath, ProtectionLevel protectionLevel, string password)
        {
            if (!_isInitialized)
                throw new Exception("You must initialize project file before you can save it.");

            var fullPath = Path.GetFullPath(filePath);
            var destinationDirectory = Path.GetDirectoryName(fullPath);
            if (destinationDirectory == null)
                throw new Exception($"Failed to determine directory of the path {filePath}.");

            Directory.CreateDirectory(destinationDirectory);

            var xmlToSave = PrepareXmlToSave(protectionLevel, password);

            xmlToSave.Save(fullPath);
        }

        private XmlDocument PrepareXmlToSave(ProtectionLevel protectionLevel, string password)
        {
            var xmlToSave = new XmlDocument();
            xmlToSave.LoadXml(FileXmlDocument.OuterXml);

            if (protectionLevel != ProtectionLevel.DontSaveSensitive && protectionLevel != ProtectionLevel.EncryptSensitiveWithPassword)
                throw new Exception($"Invalid Protection Level for Deployment Package: {protectionLevel}.");

            if (protectionLevel == ProtectionLevel.EncryptSensitiveWithPassword && string.IsNullOrWhiteSpace(password))
                throw new Exception($"Password must be specified for ProtectionLevel {protectionLevel}.");


            SetProtectionLevel(xmlToSave, protectionLevel);

            var sensitiveNodes = GetSensitiveNodes(xmlToSave);
            foreach (var sensitiveNode in sensitiveNodes)
            {
                if (protectionLevel == ProtectionLevel.EncryptSensitiveWithPassword)
                    EncryptNode(sensitiveNode, password);

                if (protectionLevel == ProtectionLevel.DontSaveSensitive)
                    sensitiveNode.ParentNode?.RemoveChild(sensitiveNode);
            }
            return xmlToSave;
        }

        protected virtual void SetProtectionLevel(XmlDocument protectedXmlDocument, ProtectionLevel protectionLevel) {}

        private void Decrypt(string password)
        {
            var encryptedNodes = GetEncryptedNodes(FileXmlDocument);
            foreach (XmlNode encryptedNode in encryptedNodes)
            {
                if (string.IsNullOrWhiteSpace(password))
                    throw new Exception("Password must be specified in order to decrypt xml.");

                DecryptNode(encryptedNode, password);
            }
        }
        protected XmlNodeList GetEncryptedNodes(XmlNode rootNode)
        {
            return rootNode.SelectNodes("//*[@Salt or @SSIS:Salt]",
                rootNode.GetNameSpaceManager());
        }

        protected virtual void EncryptNode(XmlNode node, string password)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var element = node as XmlElement;
            if (element == null)
                throw new Exception("Trying to encrypt node that is not an element.");

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

        protected IList<XmlNode> GetSensitiveNodes(XmlNode rootNode)
        {
            var sensitiveNodes = new List<XmlNode>();

            var sensitiveNodesNumberAttributeValue = rootNode.SelectNodes("//*[@Sensitive=\"1\" or @SSIS:Sensitive=\"1\"]", NamespaceManager);

            if (sensitiveNodesNumberAttributeValue != null)
                sensitiveNodes.AddRange(sensitiveNodesNumberAttributeValue.OfType<XmlNode>());

            // Package has an old way of dealing with it.
            var sensitiveNodesStringAttributeValue =
                rootNode.SelectNodes(
                    "//DTS:PackageParameter[@DTS:Sensitive=\"True\"]/DTS:Property[@DTS:Name=\"ParameterValue\"]/DTS:Property[@DTS:Name=\"ParameterValue\"]",
                    NamespaceManager);

            if (sensitiveNodesStringAttributeValue != null)
                sensitiveNodes.AddRange(sensitiveNodesStringAttributeValue.OfType<XmlNode>());

            return sensitiveNodes;
        }

        protected virtual void DecryptNode(XmlNode node, string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidPaswordException();

            if (node == null)
                throw new ArgumentNullException(nameof(node));


            var saltXmlAttribute =  node.GetAttribute("Salt");
            if (saltXmlAttribute == null)
            {
                throw new Exception($"Encrypted node {node.Name} does not contain required Attribute \"Salt\"");
            }
            byte[] rgbSalt;
            try
            {
                rgbSalt = Convert.FromBase64String(saltXmlAttribute.Value);
            }
            catch (FormatException)
            {
                throw new Exception(
                    $"Invalid value of Attribute \"Salt\" ({saltXmlAttribute.Value}) in encrypted node {node.Name} ");
            }
            var ivXmlAttribute = node.GetAttribute("IV");
            if (ivXmlAttribute == null)
            {
                throw new Exception($"Encrypted node {node.Name} does not contain required Attribute \"IV\"");
            }
            byte[] numArray;
            try
            {
                numArray = Convert.FromBase64String(ivXmlAttribute.Value);
            }
            catch (FormatException)
            {
                throw new Exception(
                    $"Invalid value of Attribute \"IV\" ({ivXmlAttribute.Value}) in encrypted node {node.Name} ");
            }
            var cryptoServiceProvider = new TripleDESCryptoServiceProvider {IV = numArray};

            var passwordDeriveBytes = new PasswordDeriveBytes(password, rgbSalt);

            cryptoServiceProvider.Key = passwordDeriveBytes.CryptDeriveKey("TripleDES", "SHA1", 192,
                cryptoServiceProvider.IV);
            string xml;

            byte[] buffer;
            try
            {
                buffer = Convert.FromBase64String(node.InnerText);
            }
            catch (FormatException)
            {
                throw new Exception(
                    $"Invalid value of encrypted node {node.Name}: {node.InnerText.Substring(0, 64)}");
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
                    $"Invalid decrypted xml in node {node.Name}: {xml.Substring(0, 64)}");
            }
            node.Attributes?.Remove(saltXmlAttribute);
            node.Attributes?.Remove(ivXmlAttribute);

            foreach (XmlNode childNode in node.ChildNodes)
                node.RemoveChild(childNode);
            node.InnerXml = xmlDocument.DocumentElement?.InnerXml;
        }

    }
}
