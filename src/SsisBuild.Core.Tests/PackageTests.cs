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
using System.Xml;
using SsisBuild.Core.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class PackageTests : IDisposable
    {
        private readonly string _workingFolder;

        public PackageTests()
        {
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingFolder);
        }

        [Fact]
        public void Pass_Get_ProtectionLevel()
        {
            var xml = CreateXml(Helpers.RandomString(20), 2, Helpers.RandomString(20));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);
            Assert.NotNull(package);
            Assert.Equal((ProtectionLevel) 2, package.ProtectionLevel);
        }

        [Fact]
        public void Fail_InvalidProtectionLevel()
        {
            var xml = CreateXml(Helpers.RandomString(20), 1000, Helpers.RandomString(20));


            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            var exception = Record.Exception(() => package.Initialize(path, null));
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
        }

        [Fact]
        public void Fail_NoProtectionLevel()
        {
            var xml = CreateXml(Helpers.RandomString(20), 1000, Helpers.RandomString(20));
            xml = xml.Replace("DTS:ProtectionLevel=\"1000\"", "");


            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            var exception = Record.Exception(() => package.Initialize(path, null));
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
        }

        [Fact]
        public void Fail_UnparsableProtectionLevel()
        {
            var xml = CreateXml(Helpers.RandomString(20), 1000, Helpers.RandomString(20));
            xml = xml.Replace("DTS:ProtectionLevel=\"1000\"", "DTS:ProtectionLevel=\"abc\"");


            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            var exception = Record.Exception(() => package.Initialize(path, null));
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
        }

        [Fact]
        public void Pass_Encrypt()
        {
            var password = Helpers.RandomString(30);
            var xml = CreateXml(Helpers.RandomString(20), 2, Helpers.RandomString(20));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);

            string encryptedXml;
            using (var stream = new MemoryStream())
            {
                package.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;
                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(encryptedXml);
            var encryptedNode = xmlDoc.SelectSingleNode("/DTS:Executable/DTS:PackageParameter/DTS:Property[@DTS:Name=\"ParameterValue\"]", xmlDoc.GetNameSpaceManager());
            Assert.True(xmlDoc.SelectNodes("//*[name(.)=\"EncryptedData\"]")?.Count == 2);
        }

        [Fact]
        public void Pass_Decrypt()
        {
            var password = Helpers.RandomString(30);
            var value = Helpers.RandomString(40);
            var value1 = Helpers.RandomString(40);
            var xml = CreateXml(value, 2, value1);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);

            var newPackage = new Package();

            using (var stream = new MemoryStream())
            {
                package.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;
                newPackage.Initialize(stream, password);
            }
            // if there is no exception - we are good.
        }

        [Fact]
        public void Fail_Decrypt_NoPassword()
        {
            var password = Helpers.RandomString(30);
            var value = Helpers.RandomString(40);
            var value1 = Helpers.RandomString(40);
            var xml = CreateXml(value, 2, value1);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);

            var newPackage = new Package();


            Exception exception;
            using (var stream = new MemoryStream())
            {
                package.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;
                exception = Record.Exception(() => newPackage.Initialize(stream, null));
            }
            Assert.NotNull(exception);
            Assert.IsType<InvalidPaswordException>(exception);
        }

        [Fact]
        public void Fail_Decrypt_BadPassword()
        {
            var password = Helpers.RandomString(30);
            var value = Helpers.RandomString(40);
            var value1 = Helpers.RandomString(40);
            var xml = CreateXml(value, 2, value1);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);

            var newPackage = new Package();


            Exception exception;
            using (var stream = new MemoryStream())
            {
                package.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;
                exception = Record.Exception(() => newPackage.Initialize(stream, Helpers.RandomString(30)));
            }
            Assert.NotNull(exception);
            Assert.IsType<InvalidPaswordException>(exception);
        }

        [Fact]
        public void Fail_Decrypt_NoIv()
        {
            var password = Helpers.RandomString(30);
            var value = Helpers.RandomString(40);
            var value1 = Helpers.RandomString(40);
            var xml = CreateXml(value, 2, value1);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);

            var newPackage = new Package();

            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                package.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var xmlNodeList = encryptedXmlDoc.SelectNodes("//*[@IV or @SSIS:IV]", encryptedXmlDoc.GetNameSpaceManager());
            if (xmlNodeList != null)
                foreach (XmlNode node in xmlNodeList)
                {
                    var ivAttribute = node.GetAttributeNode("IV");
                    if (ivAttribute != null)
                        ivAttribute.Value = string.Empty;
                }

            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newPackage.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"IV\""));
        }

        [Fact]
        public void Fail_Decrypt_BadIv()
        {
            var password = Helpers.RandomString(30);
            var value = Helpers.RandomString(40);
            var value1 = Helpers.RandomString(40);
            var xml = CreateXml(value, 2, value1);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);

            var newPackage = new Package();

            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                package.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var xmlNodeList = encryptedXmlDoc.SelectNodes("//*[@IV or @SSIS:IV]", encryptedXmlDoc.GetNameSpaceManager());
            if (xmlNodeList != null)
                foreach (XmlNode node in xmlNodeList)
                {
                    var ivAttribute = node.GetAttributeNode("IV");
                    if (ivAttribute != null)
                        ivAttribute.Value = Helpers.RandomString(30);
                }

            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newPackage.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"IV\""));
        }

        [Fact]
        public void Fail_Decrypt_NoSalt()
        {
            var password = Helpers.RandomString(30);
            var value = Helpers.RandomString(40);
            var value1 = Helpers.RandomString(40);
            var xml = CreateXml(value, 2, value1);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);

            var newPackage = new Package();

            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                package.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var xmlNodeList = encryptedXmlDoc.SelectNodes("//*[@Salt or @SSIS:Salt]", encryptedXmlDoc.GetNameSpaceManager());
            if (xmlNodeList != null)
                foreach (XmlNode node in xmlNodeList)
                {
                    var saltAttribute = node.GetAttributeNode("Salt");
                    if (saltAttribute != null)
                        saltAttribute.Value = string.Empty;
                }

            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newPackage.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"Salt\""));
        }

        [Fact]
        public void Fail_Decrypt_BadSalt()
        {
            var password = Helpers.RandomString(30);
            var value = Helpers.RandomString(40);
            var value1 = Helpers.RandomString(40);
            var xml = CreateXml(value, 2, value1);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);

            var newPackage = new Package();

            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                package.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var xmlNodeList = encryptedXmlDoc.SelectNodes("//*[@Salt or @SSIS:Salt]", encryptedXmlDoc.GetNameSpaceManager());
            if (xmlNodeList != null)
                foreach (XmlNode node in xmlNodeList)
                {
                    var saltAttribute = node.GetAttributeNode("Salt");
                    if (saltAttribute != null)
                        saltAttribute.Value = Helpers.RandomString(30);
                }

            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newPackage.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"Salt\""));
        }

        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
        }

        internal static string CreateXml(string sensitiveParameterValue, int protectionLevel, string sensitivePasswordValue)
        {
            return $@"<?xml version=""1.0""?>
                <DTS:Executable xmlns:DTS=""www.microsoft.com/SqlServer/Dts""
                DTS:ProtectionLevel=""{protectionLevel}"">
                 <DTS:ConnectionManager DTS:ConnectionString=""{Helpers.RandomString(20)}"">
                   <DTS:Password DTS:Name=""{Helpers.RandomString(20)}"" Sensitive=""1"">{sensitivePasswordValue}</DTS:Password>
                </DTS:ConnectionManager>
                <DTS:PackageParameter DTS:Sensitive=""True"">
                    <DTS:Property DTS:Name=""ParameterValue"">{sensitiveParameterValue}</DTS:Property>
                </DTS:PackageParameter>
                </DTS:Executable>";
        }
    }
}