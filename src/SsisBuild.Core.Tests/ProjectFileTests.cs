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
using System.Xml;
using SsisBuild.Core.ProjectManagement;
using SsisBuild.Core.ProjectManagement.Helpers;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    internal class ProjectFileImpl : ProjectFile
    {
        public XmlDocument FileXmlDocumentPublic => FileXmlDocument;
    }
    public class ProjectFileTests : IDisposable
    {
        private readonly string _workingFolder;

        public ProjectFileTests()
        {
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingFolder);
        }

        [Fact]
        public void Pass_Encrypt()
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };


            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);

            // Execute
            string encryptedXml;
            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;
                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(encryptedXml);
            var encryptedNode = xmlDoc.SelectSingleNode("//SSIS:Property[@SSIS:Name=\"Value\"]", xmlDoc.GetNameSpaceManager());

            // Assert - should have encrypted node and Salt attribute
            Assert.True(encryptedNode?.Attributes?["Salt", XmlHelpers.Schemas.SSIS]?.Value != null);
        }

        [Fact]
        public void Pass_Save_ToStream_NoSensitive()
        {
            // Setup
            // Use ProjectParams xml in this test
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };

            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);

            // Execute
            string encryptedXml;
            using (var stream = new MemoryStream())
            {
                // Save with DontSaveSensitive
                projectFile.Save(stream);
                stream.Position = 0;
                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }


            // Assert -- sensitive node should be gone
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(encryptedXml);
            var encryptedNode = xmlDoc.SelectSingleNode("//SSIS:Property[@SSIS:Name=\"Value\"]", xmlDoc.GetNameSpaceManager());

            Assert.Null(encryptedNode);
        }



        [Fact]
        public void Pass_Save_ToFile_NoSensitive()
        {
            // Setup
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };
            
            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);

            var saveToPath = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));

            // Execute - save with no sensitive info
            projectFile.Save(saveToPath);

            // Assert - should not have sensitive node
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(saveToPath);
            var encryptedNode = xmlDoc.SelectSingleNode("//SSIS:Property[@SSIS:Name=\"Value\"]", xmlDoc.GetNameSpaceManager());
            Assert.Null(encryptedNode);
        }
        
        [Fact]
        public void Fail_Save_NotInitialized()
        {
            // Setup
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };

            var projectFile = new ProjectFileImpl();

            // Execute
            var exception = Record.Exception(() => projectFile.Save(Fakes.RandomString()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ProjectNotInitializedException>(exception);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword)]
        public void Pass_Decrypt_FromStream(ProtectionLevel protectionLevel)
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };

            // Use ProjectParams here to later validate without having to save file xml again
            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);

            // Execute
            var newProjectFile = new ProjectFileImpl();
            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, protectionLevel, password);
                stream.Position = 0;
                newProjectFile.Initialize(stream, password);

            }

            // Assert
            var decryptedNodeValue =
                newProjectFile.FileXmlDocumentPublic.SelectSingleNode("//SSIS:Property[@SSIS:Name=\"Value\"]", newProjectFile.FileXmlDocumentPublic.GetNameSpaceManager())?.InnerText;
            Assert.Equal(parameterData.Value, decryptedNodeValue);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword)]
        public void Pass_Decrypt_FromFile(ProtectionLevel protectionLevel)
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };


            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);


            var saveToPath = Path.Combine(_workingFolder, Guid.NewGuid().ToString("D"));
            projectFile.Save(saveToPath, protectionLevel, password);

            // Execute
            var newProjectFile = new ProjectFileImpl();
            newProjectFile.Initialize(saveToPath, password);

            // Assert
            var decryptedNodeValue =
                newProjectFile.FileXmlDocumentPublic.SelectSingleNode("//SSIS:Property[@SSIS:Name=\"Value\"]", newProjectFile.FileXmlDocumentPublic.GetNameSpaceManager())?.InnerText;
            Assert.Equal(parameterData.Value, decryptedNodeValue);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptAllWithUserKey)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithUserKey)]
        [InlineData(1000)]
        public void Fail_Save_InvalidProtectionLevel(ProtectionLevel protectionLevel)
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };


            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);

            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                exception = Record.Exception(() => projectFile.Save(stream, protectionLevel, password));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidProtectionLevelException>(exception);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword)]
        public void Fail_Save_NoPassword(ProtectionLevel protectionLevel)
        {
            // Setup
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };


            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);

            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                exception = Record.Exception(() => projectFile.Save(stream, protectionLevel, null));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidPaswordException>(exception);
        }

        [Fact]
        public void Fail_Decrypt_NoSalt()
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };


            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);


            var newProjectFile = new ProjectFileImpl();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var saltAttribute = encryptedXmlDoc.SelectSingleNode("//*[@Salt or @SSIS:Salt]", encryptedXmlDoc.GetNameSpaceManager()).GetAttributeNode("Salt");
            if (saltAttribute != null)
                saltAttribute.Value = string.Empty;

            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectFile.Initialize(stream, password));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"Salt\""));
        }
        
        [Fact]
        public void Fail_Decrypt_BadSalt()
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };
            
            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);
            
            var newProjectFile = new ProjectFileImpl();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var saltAttribute = encryptedXmlDoc.SelectSingleNode("//*[@Salt or @SSIS:Salt]", encryptedXmlDoc.GetNameSpaceManager()).GetAttributeNode("Salt");
            if (saltAttribute != null)
                saltAttribute.Value = $"*{Fakes.RandomString()}"; // Added * to break Convert.FromBase64 false success

            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectFile.Initialize(stream, password));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"Salt\""));
        }

        [Fact]
        public void Fail_Decrypt_NoIv()
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };
            
            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);
            
            var newProjectFile = new ProjectFileImpl();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var ivAttribute = encryptedXmlDoc.SelectSingleNode("//*[@IV or @SSIS:IV]", encryptedXmlDoc.GetNameSpaceManager()).GetAttributeNode("IV");
            if (ivAttribute != null)
                ivAttribute.Value = string.Empty;
            
            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectFile.Initialize(stream, password));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"IV\""));
        }

        [Fact]
        public void Fail_Decrypt_BadIv()
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };


            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);


            var newProjectFile = new ProjectFileImpl();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var ivAttribute = encryptedXmlDoc.SelectSingleNode("//*[@IV or @SSIS:IV]", encryptedXmlDoc.GetNameSpaceManager()).GetAttributeNode("IV");
            if (ivAttribute != null)
                ivAttribute.Value = $"*{Fakes.RandomString()}"; // Added * to break Convert.FromBase64 false success

            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectFile.Initialize(stream, password));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("\"IV\""));
        }


        [Fact]
        public void Fail_Decrypt_BadEncryptedValue()
        {
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };


            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);


            var newProjectFile = new ProjectFileImpl();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);
            var encryptedValueNode = encryptedXmlDoc.SelectSingleNode("//*[@IV or @SSIS:IV]", encryptedXmlDoc.GetNameSpaceManager());
            if (encryptedValueNode != null)
                encryptedValueNode.InnerText = $"#_{Fakes.RandomString()}"; // to avoid false base64 false positive

            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectFile.Initialize(stream, password));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("encrypted element"));
        }

        [Fact]
        public void Fail_Decrypt_BadPassword()
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };

            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);

            var newProjectFile = new ProjectFileImpl();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);

            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectFile.Initialize(stream, Fakes.RandomString()));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidPaswordException>(exception);
        }

        [Fact]
        public void Fail_Decrypt_NoPassword()
        {
            // Setup
            var password = Fakes.RandomString();
            var parameterData = new ParameterSetupData()
            {
                Value = Fakes.RandomString(),
                DataType = DataType.String,
                Name = Fakes.RandomString(),
                Sensitive = true
            };

            var xml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(new[] { parameterData }));
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectFile = new ProjectFileImpl();
            projectFile.Initialize(path, null);


            var newProjectFile = new ProjectFileImpl();
            string encryptedXml;

            using (var stream = new MemoryStream())
            {
                projectFile.Save(stream, ProtectionLevel.EncryptSensitiveWithPassword, password);
                stream.Position = 0;

                var sr = new StreamReader(stream);
                encryptedXml = sr.ReadToEnd();
            }

            var encryptedXmlDoc = new XmlDocument();
            encryptedXmlDoc.LoadXml(encryptedXml);

            // Execute
            Exception exception;
            using (var stream = new MemoryStream())
            {
                encryptedXmlDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                exception = Record.Exception(() => newProjectFile.Initialize(stream, string.Empty));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidPaswordException>(exception);
        }
        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
        }
    }
}
