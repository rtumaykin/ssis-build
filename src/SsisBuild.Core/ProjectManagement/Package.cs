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
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using SsisBuild.Core.ProjectManagement.Helpers;

namespace SsisBuild.Core.ProjectManagement
{
    public class Package : ProjectFile
    {
        private XmlAttribute _protectionLevelAttribute;

        public override ProtectionLevel ProtectionLevel
        {
            get
            {
                return (ProtectionLevel) Enum.Parse(typeof(ProtectionLevel), _protectionLevelAttribute.Value);
            }
            set
            {
                _protectionLevelAttribute.Value = value.ToString("D"); 
            }
        }

        protected override void PostInitialize()
        {
            ResolveProtectionLevel();
        }

        private void ResolveProtectionLevel()
        {
            _protectionLevelAttribute = FileXmlDocument.SelectSingleNode("/DTS:Executable", NamespaceManager)?.Attributes?["DTS:ProtectionLevel"];

            // At least in Visual Studio 2017 (14.0.0800.98), the DTS:ProtectionLevel elemented is ommited in case it is set to DontSaveSensitive.
            if (_protectionLevelAttribute == null) {
                var attr = FileXmlDocument.CreateAttribute("DTS:ProtectionLevel");
                attr.Value = "DontSaveSensitive";
                _protectionLevelAttribute = attr;
            }
            var protectionLevelValue = _protectionLevelAttribute?.Value;

            ProtectionLevel protectionLevel;
            if (!Enum.TryParse(protectionLevelValue, true, out protectionLevel))
                throw new InvalidXmlException($"Invalid DTS:ProtectionLevel value {protectionLevelValue}.", FileXmlDocument);

            if (!Enum.IsDefined(typeof(ProtectionLevel), protectionLevel))
                throw new InvalidXmlException($"Invalid DTS:ProtectionLevel value {protectionLevelValue}.", FileXmlDocument);
        }



        protected override void EncryptElement(XmlElement element, string password)
        {
            var rgbSalt = new byte[7];
            new RNGCryptoServiceProvider().GetBytes(rgbSalt);
            var cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            var passwordDeriveBytes = new PasswordDeriveBytes(password, rgbSalt);

            cryptoServiceProvider.Key = passwordDeriveBytes.CryptDeriveKey("TripleDES", "SHA1", 192, cryptoServiceProvider.IV);

            var exml = new EncryptedXml();

            var encryptedElement = exml.EncryptData(element, cryptoServiceProvider, false);

            var encryptedData = new EncryptedData
            {
                Type = EncryptedXml.XmlEncElementUrl,
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncTripleDESUrl),
                CipherData = {CipherValue = encryptedElement}
            };


            // first we add it as a child, then move forward. I did not want to call an internal method (why do they make all useful methods internal?)
            // It is inconsistent at this level. For connection managers, it encrypts the entire element and just replaces the element's outer xml with encrypted node
            // For package parameters they leave original element with DTS:Name attribute, remove all other attributes such as DTS:DataType and then add encrypted element 
            // as an inner xml to original element. This is what I have observed, hopefully it is at least consistentl inconsistent, and there is no third way.
            EncryptedXml.ReplaceElement(element, encryptedData, true);
            var replacementElement = element.FirstChild as XmlElement;
            var parentNode = element.ParentNode;

            if (replacementElement != null && parentNode != null)
            {
                replacementElement.SetAttribute("Salt", Convert.ToBase64String(rgbSalt));
                replacementElement.SetAttribute("IV", Convert.ToBase64String(cryptoServiceProvider.IV));

                // if parent node is marked as sensitive, then it needs to be replaced. Otherwise leave the encrypted node where it is.
                if (XmlHelpers.GetAttributeNode(parentNode, "Sensitive")?.Value == null)
                {
                    parentNode.RemoveChild(element);
                    parentNode.AppendChild(replacementElement);
                }
            }

        }

        protected override void DecryptElement(XmlElement element, string password)
        {
            var saltXmlAttribute = XmlHelpers.GetAttributeNode(element, "Salt");
            if (string.IsNullOrEmpty(saltXmlAttribute?.Value))
            {
                throw new InvalidXmlException($"Encrypted node {element.Name} does not contain required Attribute \"Salt\"", element);
            }
            byte[] rgbSalt;
            try
            {
                rgbSalt = Convert.FromBase64String(saltXmlAttribute.Value);
            }
            catch (FormatException)
            {
                throw new InvalidXmlException($"Invalid value of Attribute \"Salt\" ({saltXmlAttribute.Value}) in encrypted node {element.Name} ", element);
            }
            var ivXmlAttribute = XmlHelpers.GetAttributeNode(element, "IV");
            if (string.IsNullOrEmpty(ivXmlAttribute?.Value))
            {
                throw new InvalidXmlException($"Encrypted node {element.Name} does not contain required Attribute \"IV\"", element);
            }
            byte[] numArray;
            try
            {
                numArray = Convert.FromBase64String(ivXmlAttribute.Value);
            }
            catch (FormatException)
            {
                throw new InvalidXmlException($"Invalid value of Attribute \"IV\" ({ivXmlAttribute.Value}) in encrypted node {element.Name} ", element);
            }
            var cryptoServiceProvider = new TripleDESCryptoServiceProvider { IV = numArray };

            var passwordDeriveBytes = new PasswordDeriveBytes(password, rgbSalt);

            var encryptedData = new EncryptedData();

            encryptedData.LoadXml(element);


            cryptoServiceProvider.Key = passwordDeriveBytes.CryptDeriveKey("TripleDES", "SHA1", 192,
                cryptoServiceProvider.IV);

            // weird edge case - if this is a parameter value, then it must replace one more parent level up
            var elementToReplace = element.ParentNode?.Name == "DTS:Property" && (element.ParentNode as XmlElement) != null && element.ParentNode?.ParentNode?.Name == "DTS:PackageParameter"
                ? (XmlElement)element.ParentNode
                : element;

            var exml = new EncryptedXml();
            try
            {
                var output = exml.DecryptData(encryptedData, cryptoServiceProvider);
                exml.ReplaceData(elementToReplace, output);
            }
            catch (CryptographicException)
            {
                throw new InvalidPaswordException(); 
            }
        }
    }
}
