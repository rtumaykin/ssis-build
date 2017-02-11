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
using System.IO;
using System.Linq;
using System.Xml;
using SsisBuild.Core.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ProjectParamsTests : IDisposable
    {
        private readonly string _workingFolder;

        public class ParameterSetupData
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool Sensitive { get; set; }
            public DataType DataType { get; set; }
        }

        public ProjectParamsTests()
        {
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingFolder);
        }

        [Theory, MemberData(nameof(ParameterData))]
        public void Pass_New(IList<ParameterSetupData> parameters)
        {
            var xml = CreateXml(parameters);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);
            Assert.NotNull(projectParams);

            foreach (var parameterSetupData in parameters)
            {
                var fullName = $"Project::{parameterSetupData.Name}";
                Assert.True(projectParams.Parameters.ContainsKey(fullName));
                Assert.Equal(parameterSetupData.Value, projectParams.Parameters[fullName].Value);
                Assert.Equal(parameterSetupData.Sensitive, projectParams.Parameters[fullName].Sensitive);
                Assert.Equal(parameterSetupData.DataType.ToString("G"), projectParams.Parameters[fullName].ParameterDataType.Name);
            }
        }

        [Fact]
        public void Pass_Encrypt()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] {parameterData}));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);

            string encryptedXml;
            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;
                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(encryptedXml);
            var encryptedNode = xmlDoc.SelectSingleNode("//SSIS:Property[@SSIS:Name=\"Value\"]", xmlDoc.GetNameSpaceManager());
            Assert.True(encryptedNode?.Attributes?["Salt", XmlHelpers.Schemas.SSIS]?.Value != null);
        }

        [Fact]
        public void Pass_Save_NoSensitive()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);

            string encryptedXml;
            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream);
                stream.Position = 0;
                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(encryptedXml);
            var encryptedNode = xmlDoc.SelectSingleNode("//SSIS:Property[@SSIS:Name=\"Value\"]", xmlDoc.GetNameSpaceManager());
            Assert.Null(encryptedNode);
        }

        [Fact]
        public void Pass_Save_ToFile_NoSensitive()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);

            var saveToPath = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));

            projectParams.Save(saveToPath);

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(saveToPath);
            var encryptedNode = xmlDoc.SelectSingleNode("//SSIS:Property[@SSIS:Name=\"Value\"]", xmlDoc.GetNameSpaceManager());
            Assert.Null(encryptedNode);
        }

        [Fact]
        public void Fail_Save_NotInitialized()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();

            var exception = Record.Exception(() => projectParams.Save(Helpers.RandomString(20)));

            Assert.NotNull(exception);
            Assert.IsType<ProjectNotInitializedException>(exception);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword)]
        public void Pass_Decrypt(ProtectionLevel protectionLevel)
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, protectionLevel, password);
                stream.Position = 0;
                newProjectParams.Initialize(stream, password);

            }
            Assert.Equal(1, newProjectParams.Parameters.Count);
            Assert.True(newProjectParams.Parameters.ContainsKey($"Project::{parameterData.Name}"));
            Assert.Equal(parameterData.Value, newProjectParams.Parameters[$"Project::{parameterData.Name}"].Value);
        }


        [Theory]
        [InlineData(ProtectionLevel.EncryptAllWithUserKey)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithUserKey)]
        [InlineData(1000)]
        public void Fail_Save_InvalidProtectionLevel(ProtectionLevel protectionLevel)
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] {parameterData}));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            Exception exception;

            using (var stream = new MemoryStream())
            {
                exception = Record.Exception(() => projectParams.Save(stream, protectionLevel, password));
            }
            Assert.NotNull(exception);
            Assert.IsType<InvalidProtectionLevelException>(exception);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword)]
        public void Fail_Save_NoPassword(ProtectionLevel protectionLevel)
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            Exception exception;

            using (var stream = new MemoryStream())
            {
                exception = Record.Exception(() => projectParams.Save(stream, protectionLevel, null));
            }
            Assert.NotNull(exception);
            Assert.IsType<InvalidPaswordException>(exception);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword)]
        public void Pass_Decrypt_FromFile(ProtectionLevel protectionLevel)
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var saveToPath = Path.Combine(_workingFolder, Guid.NewGuid().ToString("D"));
            projectParams.Save(saveToPath, protectionLevel, password);

            var newProjectParams = new ProjectParams();
            newProjectParams.Initialize(saveToPath, password);

            Assert.Equal(1, newProjectParams.Parameters.Count);
            Assert.True(newProjectParams.Parameters.ContainsKey($"Project::{parameterData.Name}"));
            Assert.Equal(parameterData.Value, newProjectParams.Parameters[$"Project::{parameterData.Name}"].Value);
        }

        [Fact]
        public void Fail_Decrypt_NoSalt()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var saltAttribute = encryptedXmlDoc.SelectSingleNode("//*[@Salt or @SSIS:Salt]", encryptedXmlDoc.GetNameSpaceManager()).GetAttributeNode("Salt");
            if (saltAttribute != null)
                saltAttribute.Value = string.Empty;


            Exception exception;

            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectParams.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"Salt\""));
        }

        [Fact]
        public void Fail_Decrypt_BadSalt()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var saltAttribute = encryptedXmlDoc.SelectSingleNode("//*[@Salt or @SSIS:Salt]", encryptedXmlDoc.GetNameSpaceManager()).GetAttributeNode("Salt");
            if (saltAttribute != null)
                saltAttribute.Value = Helpers.RandomString(30);


            Exception exception;

            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectParams.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"Salt\""));
        }

        [Fact]
        public void Fail_Decrypt_NoIv()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var ivAttribute = encryptedXmlDoc.SelectSingleNode("//*[@IV or @SSIS:IV]", encryptedXmlDoc.GetNameSpaceManager()).GetAttributeNode("IV");
            if (ivAttribute != null)
                ivAttribute.Value = string.Empty;


            Exception exception;

            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectParams.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"IV\""));
        }

        [Fact]
        public void Fail_Decrypt_BadIv()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var ivAttribute = encryptedXmlDoc.SelectSingleNode("//*[@IV or @SSIS:IV]", encryptedXmlDoc.GetNameSpaceManager()).GetAttributeNode("IV");
            if (ivAttribute != null)
                ivAttribute.Value = Helpers.RandomString(30);


            Exception exception;

            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectParams.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"IV\""));
        }

        [Fact]
        public void Fail_Decrypt_BadEncryptedValue()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var encryptedValueNode = encryptedXmlDoc.SelectSingleNode("//*[@IV or @SSIS:IV]", encryptedXmlDoc.GetNameSpaceManager());
            if (encryptedValueNode != null)
                encryptedValueNode.InnerText = Helpers.RandomString(30);


            Exception exception;

            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectParams.Initialize(stream, password));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("encrypted element"));
        }

        [Fact]
        public void Fail_Decrypt_BadPassword()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);

            Exception exception;

            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectParams.Initialize(stream, Helpers.RandomString(30)));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidPaswordException>(exception);
        }

        [Fact]
        public void Fail_Decrypt_NoPassword()
        {
            var password = Helpers.RandomString(30);
            var parameterData = new ParameterSetupData()
            {
                Value = Helpers.RandomString(10),
                DataType = DataType.String,
                Name = Helpers.RandomString(20),
                Sensitive = true
            };


            var xml = CreateXml(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);


            var newProjectParams = new ProjectParams();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectParams.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);

            Exception exception;

            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectParams.Initialize(stream, string.Empty));
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidPaswordException>(exception);
        }
        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
        }

        internal static string CreateXml(IList<ProjectParamsTests.ParameterSetupData> parameters )
        {
            return $@"<?xml version=""1.0""?>
                <SSIS:Parameters xmlns:SSIS=""www.microsoft.com/SqlServer/SSIS"">
                {string.Join("", parameters.Select(p=>CreateParameterXml(p.Name, p.Value, p.Sensitive, p.DataType)))}
            </SSIS:Parameters>";
        }

        internal static string CreateParameterXml(string name, string value, bool sensitive, DataType dataType)
        {
            var sensitiveInt = sensitive ? 1 : 0;
            var sensitiveAttr = sensitive ? "SSIS:Sensitive =\"1\"" : null;

            return $@"<SSIS:Parameter SSIS:Name=""{name}"">
                <SSIS:Properties>
                  <SSIS:Property
                    SSIS:Name=""ID"">{Guid.NewGuid():B}</SSIS:Property>
                  <SSIS:Property
                    SSIS:Name=""CreationName""></SSIS:Property>
                  <SSIS:Property
                    SSIS:Name=""Description""></SSIS:Property>
                  <SSIS:Property
                    SSIS:Name=""IncludeInDebugDump"">0</SSIS:Property>
                  <SSIS:Property
                    SSIS:Name=""Required"">0</SSIS:Property>
                  <SSIS:Property
                    SSIS:Name=""Sensitive"">{sensitiveInt}</SSIS:Property>
                  <SSIS:Property
                    SSIS:Name=""Value"" {sensitiveAttr}>{value}</SSIS:Property>
                  <SSIS:Property
                    SSIS:Name=""DataType"">{dataType:D}</SSIS:Property>
                </SSIS:Properties>
              </SSIS:Parameter>";
        }

        private static IEnumerable<object[]> ParameterData()
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            var testsCount = rnd.Next(10, 40);
            for (var cnt = 0; cnt < testsCount; cnt++)
            {
                var paramsCount = rnd.Next(0, 20);
                var paramsData = new List<ParameterSetupData>();
                for (var cnt1 = 0; cnt1 < paramsCount; cnt1++)
                {
                    paramsData.Add(new ParameterSetupData()
                    {
                        Name = Helpers.RandomString(20),
                        Value = Helpers.RandomString(30),
                        DataType = DataType.String,
                        Sensitive = rnd.Next(0, 1000) < 500
                    });
                }

                yield return new object[] {paramsData};
            }
            yield return new object[] { new List<ParameterSetupData>() };
        }
    }
}