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
using SsisBuild.Core.ProjectManagement;
using SsisBuild.Core.ProjectManagement.Helpers;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ProjectManifestTests : IDisposable
    {
        private readonly string _workingFolder;

        public ProjectManifestTests()
        {
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingFolder);
        }
        
        [Theory, MemberData(nameof(ParameterData))]
        public void Pass_New(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);

            // Execute
            var projectManifest = new ProjectManifest();
            projectManifest.Initialize(path, null);

            // Assert
            Assert.NotNull(projectManifest);

            foreach (var parameterSetupData in parameters)
            {
                var fullName = $"Project::{parameterSetupData.Name}";
                Assert.True(projectManifest.Parameters.ContainsKey(fullName));
                Assert.Equal(parameterSetupData.Value, projectManifest.Parameters[fullName].Value);
                Assert.Equal(parameterSetupData.Sensitive, projectManifest.Parameters[fullName].Sensitive);
                Assert.Equal(parameterSetupData.DataType.ToString("G"), projectManifest.Parameters[fullName].ParameterDataType.Name);
            }

            foreach (var package in packages)
            {
                Assert.True(projectManifest.PackageNames.Contains(package));
                foreach (var parameterSetupData in parameters)
                {
                    var fullName = $"{package}::{parameterSetupData.Name}";
                    Assert.True(projectManifest.Parameters.ContainsKey(fullName));
                    Assert.Equal(parameterSetupData.Value, projectManifest.Parameters[fullName].Value);
                    Assert.Equal(parameterSetupData.Sensitive, projectManifest.Parameters[fullName].Sensitive);
                    Assert.Equal(parameterSetupData.DataType.ToString("G"), projectManifest.Parameters[fullName].ParameterDataType.Name);
                }
            }

            foreach (var connectionManager in connectionManagers)
            {
                Assert.True(projectManifest.ConnectionManagerNames.Contains(connectionManager));
            }

            Assert.Equal(versionMajor, projectManifest.VersionMajor);
            Assert.Equal(versionMinor, projectManifest.VersionMinor);
            Assert.Equal(versionBuild, projectManifest.VersionBuild);
            Assert.Equal(versionComments, projectManifest.VersionComments);
            Assert.Equal(description, projectManifest.Description);
            Assert.Equal(protectionLevel, projectManifest.ProtectionLevel);
        }

        [Theory, MemberData(nameof(ParameterData))]
        public void Pass_SetProtectionLevel(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description,
            string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {

            // Setup
            var xml = XmlGenerators.ProjectManifestFile(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers,
                parameters);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectManifest = new ProjectManifest();
            projectManifest.Initialize(path, null);

            var desiredProtectionLevel = projectManifest.ProtectionLevel == ProtectionLevel.DontSaveSensitive
                ? ProtectionLevel.ServerStorage
                : ProtectionLevel.DontSaveSensitive;

            string savedXml;

            using (var stream = new MemoryStream())
            {
                projectManifest.Save(stream, desiredProtectionLevel, Fakes.RandomString());
                stream.Flush();
                stream.Position = 0;

                var sr = new StreamReader(stream);
                savedXml = sr.ReadToEnd();
            }

            // Execute
            var testXmlDoc = new XmlDocument();
            testXmlDoc.LoadXml(savedXml);
            var projectProtectionLevel = testXmlDoc.DocumentElement?.Attributes["SSIS:ProtectionLevel"]?.Value;
            var packageProtectionLevelNodes = testXmlDoc.SelectNodes("//SSIS:Properties/SSIS:Property[@SSIS:Name = \"ProtectionLevel\"]", testXmlDoc.GetNameSpaceManager());

            // Assert
            ProtectionLevel testProtectionLevel;
            Assert.True(Enum.TryParse(projectProtectionLevel, out testProtectionLevel));
            Assert.Equal(desiredProtectionLevel, testProtectionLevel);

            Assert.NotNull(packageProtectionLevelNodes);

            foreach (XmlElement packageProtectionElement in packageProtectionLevelNodes)
            {
                Assert.Equal(desiredProtectionLevel, (ProtectionLevel) int.Parse(packageProtectionElement.InnerText));
            }

            Assert.Equal(desiredProtectionLevel, projectManifest.ProtectionLevel);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptAllWithUserKey)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithUserKey)]
        public void Fail_UserKeyProtectionLevel(ProtectionLevel protectionLevel)
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(protectionLevel, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] {}, new string[] {}, new ParameterSetupData[] {});
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidProtectionLevelException>(exception);
            Assert.Equal(((InvalidProtectionLevelException) exception).ProtectionLevel, protectionLevel);
        }

        [Fact]
        public void Fail_NoVersionMajor()
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] { }, new string[] { }, new ParameterSetupData[] { });
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var versionMajorNode = xmlDoc.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMajor\"]", xmlDoc.GetNameSpaceManager());
            versionMajorNode?.ParentNode?.RemoveChild(versionMajorNode);

            File.WriteAllText(path, xmlDoc.OuterXml);
            
            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Major"));
        }

        [Fact]
        public void Fail_NoVersionMinor()
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] { }, new string[] { }, new ParameterSetupData[] { });
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var versionMinorNode = xmlDoc.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMinor\"]", xmlDoc.GetNameSpaceManager());
            versionMinorNode?.ParentNode?.RemoveChild(versionMinorNode);

            File.WriteAllText(path, xmlDoc.OuterXml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Minor"));
        }

        [Fact]
        public void Fail_NoVersionBuild()
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] { }, new string[] { }, new ParameterSetupData[] { });
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var versionBuildNode = xmlDoc.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionBuild\"]", xmlDoc.GetNameSpaceManager());
            versionBuildNode?.ParentNode?.RemoveChild(versionBuildNode);

            File.WriteAllText(path, xmlDoc.OuterXml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Build"));
        }


        [Fact]
        public void Fail_InvalidVersionMajor()
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] { }, new string[] { }, new ParameterSetupData[] { });
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var versionMajorNode = xmlDoc.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMajor\"]", xmlDoc.GetNameSpaceManager());
            if (versionMajorNode != null)
                versionMajorNode.InnerText = Fakes.RandomString();

            File.WriteAllText(path, xmlDoc.OuterXml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Major"));
        }

        [Fact]
        public void Fail_InvalidVersionMinor()
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] { }, new string[] { }, new ParameterSetupData[] { });
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var versionMinorNode = xmlDoc.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMinor\"]", xmlDoc.GetNameSpaceManager());
            if (versionMinorNode != null)
                versionMinorNode.InnerText = Fakes.RandomString(); ;

            File.WriteAllText(path, xmlDoc.OuterXml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Minor"));
        }

        [Fact]
        public void Fail_InvalidVersionBuild()
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] { }, new string[] { }, new ParameterSetupData[] { });
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var versionBuildNode = xmlDoc.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionBuild\"]", xmlDoc.GetNameSpaceManager());
            if (versionBuildNode != null)
                versionBuildNode.InnerText = Fakes.RandomString(); ;

            File.WriteAllText(path, xmlDoc.OuterXml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Build"));
        }

        [Fact]
        public void Fail_NoVersionComments()
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] { }, new string[] { }, new ParameterSetupData[] { });
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var versionCommentsNode = xmlDoc.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionComments\"]", xmlDoc.GetNameSpaceManager());
            versionCommentsNode?.ParentNode?.RemoveChild(versionCommentsNode);

            File.WriteAllText(path, xmlDoc.OuterXml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Comments"));
        }

        [Fact]
        public void Fail_NoDescription()
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 1, Fakes.RandomString(), 1, Fakes.RandomString(), new string[] { }, new string[] { }, new ParameterSetupData[] { });
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var descriptionNode = xmlDoc.SelectSingleNode("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"Description\"]", xmlDoc.GetNameSpaceManager());
            descriptionNode?.ParentNode?.RemoveChild(descriptionNode);

            File.WriteAllText(path, xmlDoc.OuterXml);
            
            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Description"));
        }

        [Theory, MemberData(nameof(ParameterData))]
        public void Pass_SetVersion(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            // Setup
            var xml = XmlGenerators.ProjectManifestFile(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);

            // Execute
            var projectManifest = new ProjectManifest();
            projectManifest.Initialize(path, null);
            
            var newVersionMajor = projectManifest.VersionMajor * 2;
            var newVersionMinor = projectManifest.VersionMinor * 2;
            var newVersionBuild = projectManifest.VersionBuild * 2;
            var newVersionComments = Fakes.RandomString();
            var newDescription = Fakes.RandomString();

            projectManifest.VersionBuild = newVersionBuild;
            projectManifest.VersionMajor = newVersionMajor;
            projectManifest.VersionMinor = newVersionMinor;
            projectManifest.VersionComments = newVersionComments;
            projectManifest.Description = newDescription;

            string savedXml;

            using (var stream = new MemoryStream())
            {
                projectManifest.Save(stream, ProtectionLevel.DontSaveSensitive, Fakes.RandomString());
                stream.Flush();
                stream.Position = 0;

                var sr = new StreamReader(stream);
                savedXml = sr.ReadToEnd();
            }
            var testXmlDoc = new XmlDocument();
            testXmlDoc.LoadXml(savedXml);

            var versionMajorNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMajor\"]", testXmlDoc.GetNameSpaceManager());
            var versionMinorNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMinor\"]", testXmlDoc.GetNameSpaceManager());
            var versionBuildNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionBuild\"]", testXmlDoc.GetNameSpaceManager());
            var versionCommentsNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionComments\"]", testXmlDoc.GetNameSpaceManager());
            var descriptionNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"Description\"]", testXmlDoc.GetNameSpaceManager());

            // Assert
            Assert.NotNull(versionMajorNodes);
            foreach (XmlElement versionMajorNode in versionMajorNodes)
            {
                Assert.Equal(newVersionMajor, int.Parse(versionMajorNode.InnerText));
            }

            Assert.NotNull(versionMinorNodes);
            foreach (XmlElement versionMinorNode in versionMinorNodes)
            {
                Assert.Equal(newVersionMinor, int.Parse(versionMinorNode.InnerText));
            }

            Assert.NotNull(versionBuildNodes);
            foreach (XmlElement versionBuildNode in versionBuildNodes)
            {
                Assert.Equal(newVersionBuild, int.Parse(versionBuildNode.InnerText));
            }

            Assert.NotNull(versionCommentsNodes);
            foreach (XmlElement versionCommentsNode in versionCommentsNodes)
            {
                Assert.Equal(newVersionComments, versionCommentsNode.InnerText);
            }

            Assert.NotNull(descriptionNodes);
            foreach (XmlElement descriptionNode in descriptionNodes)
            {
                Assert.Equal(newDescription, descriptionNode.InnerText);
            }


            Assert.Equal(newVersionBuild, projectManifest.VersionBuild);
            Assert.Equal(newVersionMajor, projectManifest.VersionMajor);
            Assert.Equal(newVersionMinor, projectManifest.VersionMinor);
            Assert.Equal(newVersionComments, projectManifest.VersionComments);
            Assert.Equal(newDescription, projectManifest.Description);

        }

        [Theory, MemberData(nameof(ParameterData))]
        public void Fail_MissingProtectionLevel(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            // Setup
            var projectManifestXmlDoc = new XmlDocument();
            var xml = XmlGenerators.ProjectManifestFile(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            projectManifestXmlDoc.LoadXml(xml);
            var protectionLevelAttribute = projectManifestXmlDoc.DocumentElement?.Attributes["SSIS:ProtectionLevel"];
            protectionLevelAttribute?.OwnerElement?.Attributes.Remove(protectionLevelAttribute);

            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, projectManifestXmlDoc.OuterXml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("SSIS:ProtectionLevel"));
        }

        [Theory, MemberData(nameof(ParameterData))]
        public void Fail_InvalidProtectionLevelString(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            // Setup
            var projectManifestXmlDoc = new XmlDocument();
            var xml = XmlGenerators.ProjectManifestFile(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            projectManifestXmlDoc.LoadXml(xml);
            projectManifestXmlDoc.DocumentElement.Attributes["SSIS:ProtectionLevel"].Value = Fakes.RandomString();

            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, projectManifestXmlDoc.OuterXml);

            // Execute
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Invalid Protection Level"));
        }


        internal static IEnumerable<object[]> ParameterData()
        {
            var protectionLevels = new[] {ProtectionLevel.DontSaveSensitive, ProtectionLevel.EncryptSensitiveWithPassword, ProtectionLevel.EncryptAllWithPassword};

            var testsCount = Fakes.RandomInt(1, 20);
            for (var cnt = 0; cnt < testsCount; cnt++)
            {
                var paramsCount = Fakes.RandomInt(0, 20);
                var paramsData = new List<ParameterSetupData>();
                for (var cnt1 = 0; cnt1 < paramsCount; cnt1++)
                {
                    paramsData.Add(new ParameterSetupData()
                    {
                        Name = Fakes.RandomString(),
                        Value = Fakes.RandomString(),
                        DataType = DataType.String,
                        Sensitive = Fakes.RandomBool()
                    });
                }

                var packages = new List<string>();
                var packageNum = Fakes.RandomInt(0, 20);
                for (var cnt1 = 0; cnt1 < packageNum; cnt1++)
                {
                    packages.Add(Fakes.RandomString());
                }

                var connections = new List<string>();
                var connectionsNum = Fakes.RandomInt(0, 20);
                for (var cnt1 = 0; cnt1 < connectionsNum; cnt1++)
                {
                    connections.Add(Fakes.RandomString());
                }



                //ProtectionLevel protectionLevel, 
                //string versionMajor, 
                //string versionMinor, 
                //string versionComments, 
                //string versionBuild, 
                //string description, 
                //string[] packages, 
                //string[] connectionManagers, 
                //ParameterSetupData[] parameters

                yield return new object[]
                {
                    protectionLevels[Fakes.RandomInt(0, 299) / 100],
                    Fakes.RandomInt(0, 100),
                    Fakes.RandomInt(0, 100),
                    Fakes.RandomInt(0, 100) < 30 ? string.Empty : Fakes.RandomString(),
                    Fakes.RandomInt(0, 100),
                    Fakes.RandomInt(0, 100) < 30 ? string.Empty : Fakes.RandomString(),
                    packages.ToArray(),
                    connections.ToArray(),
                    paramsData.ToArray()
                };
            }
        }

        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
        }
    }
}