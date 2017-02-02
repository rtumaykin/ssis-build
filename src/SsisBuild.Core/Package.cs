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
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using SsisBuild.Core.Helpers;

namespace SsisBuild.Core
{
    public class Package : ProjectFile
    {
        public ProtectionLevel ProtectionLevel { get; private set; }

        protected override void PostInitialize()
        {
            ProtectionLevel = ResolveProtectionLevel();
        }

        private ProtectionLevel ResolveProtectionLevel()
        {

            var protectionLevelValue =
                FileXmlDocument.SelectSingleNode("/DTS:Executable", NamespaceManager)?.Attributes?["DTS:ProtectionLevel"]?.Value;

            if (protectionLevelValue == null)
                throw new InvalidXmlException("Failed to determine protection level. DTS:ProtectionLevel attribute was not found.", FileXmlDocument);

            ProtectionLevel protectionLevel;
            if (!Enum.TryParse(protectionLevelValue, true, out protectionLevel))
                throw new InvalidXmlException($"Invalid DTS:ProtectionLevel value {protectionLevelValue}.", FileXmlDocument);

            if (!Enum.IsDefined(typeof(ProtectionLevel), protectionLevel))
                throw new InvalidXmlException($"Invalid DTS:ProtectionLevel value {protectionLevelValue}.", FileXmlDocument);

            return protectionLevel;
        }



        protected override void EncryptNode(XmlNode node, string password)
        {
            var elementToEncrypt = node as XmlElement;
            if (elementToEncrypt == null)
                throw new Exception("Requested node is not an element.");


            var rgbSalt = new byte[7];
            new RNGCryptoServiceProvider().GetBytes(rgbSalt);
            var cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            var passwordDeriveBytes = new PasswordDeriveBytes(password, rgbSalt);

            cryptoServiceProvider.Key = passwordDeriveBytes.CryptDeriveKey("TripleDES", "SHA1", 192, cryptoServiceProvider.IV);

            var exml = new EncryptedXml();

            var encryptedElement = exml.EncryptData(elementToEncrypt, cryptoServiceProvider, false);

            var encryptedData = new EncryptedData
            {
                Type = EncryptedXml.XmlEncElementUrl,
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncTripleDESUrl),
                CipherData = {CipherValue = encryptedElement}
            };


            // first we add it as a child, then move forward. I did not want to call an internal method (why do they make all useful methods internal?)
            EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, true);
            var replacementElement = elementToEncrypt.FirstChild as XmlElement;
            var parentNode = elementToEncrypt.ParentNode;

            if (replacementElement == null)
                throw new Exception("Encryption failed");

            replacementElement.SetAttribute("Salt", Convert.ToBase64String(rgbSalt));
            replacementElement.SetAttribute("IV", Convert.ToBase64String(cryptoServiceProvider.IV));


            if (parentNode != null && parentNode.GetAttribute("Sensitive")?.Value == null)
            {
                parentNode.RemoveChild(elementToEncrypt);
                parentNode.AppendChild(replacementElement);
            }

        }

        protected override void SetProtectionLevel(XmlDocument protectedXmlDocument, ProtectionLevel protectionLevel)
        {
            var protectionLevelAttribute = protectedXmlDocument.SelectSingleNode("/DTS:Executable", NamespaceManager)?.Attributes?["DTS:ProtectionLevel"];
            if (protectionLevelAttribute != null)
                protectionLevelAttribute.Value = ((int) protectionLevel).ToString(CultureInfo.InvariantCulture);
        }

        protected override void DecryptNode(XmlNode node, string password)
        {

            if (string.IsNullOrEmpty(password))
                throw new InvalidPaswordException();


            var saltXmlAttribute = node.GetAttribute("Salt");
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
            var cryptoServiceProvider = new TripleDESCryptoServiceProvider { IV = numArray };

            var passwordDeriveBytes = new PasswordDeriveBytes(password, rgbSalt);

            var encryptedData = new EncryptedData();
            var encryptedElement = node as XmlElement;

            if (encryptedElement == null)
                throw new Exception();

            encryptedData.LoadXml(encryptedElement);


            cryptoServiceProvider.Key = passwordDeriveBytes.CryptDeriveKey("TripleDES", "SHA1", 192,
                cryptoServiceProvider.IV);

            // weird edge case - if this is a parameter value, then it must replace one more parent level up
            var elementToReplace = encryptedElement.ParentNode?.Name == "DTS:Property" && (encryptedElement.ParentNode as XmlElement) != null && encryptedElement.ParentNode?.ParentNode?.Name == "DTS:PackageParameter"
                ? (XmlElement) encryptedElement.ParentNode
                : encryptedElement;

            var exml = new EncryptedXml();
            var output = exml.DecryptData(encryptedData, cryptoServiceProvider);
            exml.ReplaceData(elementToReplace, output);
        }
    }
}
