using System;
using System.IO;
using System.Linq;
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
            var xml = CreateXml("Anything", 2);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            package.Initialize(path, null);
            Assert.NotNull(package);
            Assert.Equal((ProtectionLevel) 2, package.ProtectionLevel);
        }

        [Fact]
        public void Pass_Fail_InvalidProtectionLevel()
        {
            var xml = CreateXml("Anything", 1000);


            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var package = new Package();
            var exception = Record.Exception(() => package.Initialize(path, null));
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
        }

        [Fact]
        public void Pass_Fail_NoProtectionLevel()
        {
            var xml = CreateXml("Anything", 1000);
            xml = xml.Replace("DTS:ProtectionLevel=\"{protectionLevel}\"", "");


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
            var xml = CreateXml("Anything", 2);
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
            Assert.True(encryptedNode?.ChildNodes.OfType<XmlElement>().Any(n => n.Name == "EncryptedData"));
        }

        [Fact]
        public void Pass_Decrypt()
        {
            var password = Helpers.RandomString(30);
            var value = Helpers.RandomString(40);
            var xml = CreateXml(value, 2);
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


        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
        }

        private static string CreateXml(string sensitiveValue, int protectionLevel)
        {
            return $@"<?xml version=""1.0""?>
                <DTS:Executable xmlns:DTS=""www.microsoft.com/SqlServer/Dts""
                DTS:ProtectionLevel=""{protectionLevel}"">
                <DTS:PackageParameter DTS:Sensitive=""True"">
                    <DTS:Property DTS:Name=""ParameterValue"">{sensitiveValue}</DTS:Property>
                </DTS:PackageParameter>
                </DTS:Executable>";
        }
    }
}