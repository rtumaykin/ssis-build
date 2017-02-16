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
using System.Reflection;
using System.Xml;
using Moq;
using SsisBuild.Core.Helpers;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ProjectTests : IDisposable
    {
        private string _workingFolder;

        public ProjectTests()
        {
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
            Directory.CreateDirectory(_workingFolder);
        }

        [Fact]
        public void Pass_DtProj()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.dtproj";
            var configurationName = Fakes.RandomString();
            CreateDtprojFiles(projectName, configurationName);
            var proj = new Project();

            // Execute
            proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), configurationName, Fakes.RandomString());

            // Assert
        }

        [Fact]
        public void Fail_LoadFromDtproj_FileNotFound()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.dtproj";
            var proj = new Project();

            // Execute
            var exception = Record.Exception(() => proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), Fakes.RandomString(), Fakes.RandomString()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<FileNotFoundException>(exception);
        }

        [Fact]
        public void Fail_LoadFromDtproj_BadExtension()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.xyz";
            var projectPath = Path.Combine(_workingFolder, projectName);
            File.Create(projectPath).Close();
            var proj = new Project();

            // Execute
            var exception = Record.Exception(() => proj.LoadFromDtproj(projectPath, Fakes.RandomString(), Fakes.RandomString()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidExtensionException>(exception);
        }


        [Fact]
        public void Fail_LoadFromDtproj_NoManifest()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.dtproj";
            var projectPath = Path.Combine(_workingFolder, projectName);
            var configurationName = Fakes.RandomString();
            CreateDtprojFiles(projectName, configurationName);
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectPath);
            var manifestNode = xmlDoc.SelectSingleNode("//SSIS:Project", xmlDoc.GetNameSpaceManager());
            var parent = manifestNode?.ParentNode;
            parent?.RemoveChild(manifestNode);
            xmlDoc.Save(projectPath);

            var proj = new Project();

            // Execute
            var exception = Record.Exception(() => proj.LoadFromDtproj(projectPath, configurationName, Fakes.RandomString()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Manifest"));
        }

        [Fact]
        public void Fail_UpdateParameter_NotLoaded()
        {
            // Setup
            var proj = new Project();

            // Execute
            var exception = Record.Exception(() => proj.UpdateParameter(Fakes.RandomString(), Fakes.RandomString(), ParameterSource.Configuration));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ProjectNotInitializedException>(exception);
        }

        [Fact]
        public void Fail_Dtproj_InvalidDeploymentModel()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.dtproj";
            var projectPath = Path.Combine(_workingFolder, projectName);
            var configurationName = Fakes.RandomString();
            CreateDtprojFiles(projectName, configurationName);
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectPath);
            var deploymentModelNode = xmlDoc.SelectSingleNode("/Project/DeploymentModel", xmlDoc.GetNameSpaceManager());
            if (deploymentModelNode != null)
                deploymentModelNode.InnerText = Fakes.RandomString();
            xmlDoc.Save(projectPath);

            var proj = new Project();

            // Execute
            var exception = Record.Exception(() => proj.LoadFromDtproj(projectPath, configurationName, Fakes.RandomString()));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidDeploymentModelException>(exception);
        }

        [Fact]
        public void Pass_Save()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.dtproj";
            var projectPath = Path.Combine(_workingFolder, projectName);
            var configurationName = Fakes.RandomString();
            CreateDtprojFiles(projectName, configurationName);
            var proj = new Project();
            proj.LoadFromDtproj(projectPath, configurationName, Fakes.RandomString());

            // Execute
            var exception = Record.Exception(() => proj.Save(Path.ChangeExtension(projectPath, ".ispac")));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Fail_Save_NotInitialized_ToFile()
        {
            // Setup
            var ispacName = $"proj_{Fakes.RandomString()}.ispac";
            var proj = new Project();

            // Execute
            var exception = Record.Exception(() => proj.Save(Path.Combine(_workingFolder, ispacName)));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ProjectNotInitializedException>(exception);
        }

        [Fact]
        public void Fail_Save_NotInitialized_ToStream()
        {
            // Setup
            var ispacName = $"proj_{Fakes.RandomString()}.ispac";
            var proj = new Project();

            Exception exception;
            using (var stream = File.OpenWrite(Path.Combine(_workingFolder, ispacName)))
            {
                // Execute
                exception = Record.Exception(() => proj.Save(stream, ProtectionLevel.DontSaveSensitive, null));
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ProjectNotInitializedException>(exception);
        }

        [Fact]
        public void Fail_Save_InvalidExtension()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.dtproj";
            var projectPath = Path.Combine(_workingFolder, projectName);
            var configurationName = Fakes.RandomString();
            CreateDtprojFiles(projectName, configurationName);
            var proj = new Project();
            proj.LoadFromDtproj(projectPath, configurationName, Fakes.RandomString());

            // Execute
            var exception = Record.Exception(() => proj.Save(Path.ChangeExtension(projectPath, ".xyz")));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidExtensionException>(exception);
        }

        [Fact]
        public void Pass_LoadFromIspac()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.dtproj";
            var projectPath = Path.Combine(_workingFolder, projectName);
            var configurationName = Fakes.RandomString();
            CreateDtprojFiles(projectName, configurationName);
            var proj = new Project();
            proj.LoadFromDtproj(projectPath, configurationName, Fakes.RandomString());
            proj.Save(Path.ChangeExtension(projectPath, ".ispac"));
            var newProj = new Project();
            
            // Execute
            var exception = Record.Exception(() => newProj.LoadFromIspac(Path.ChangeExtension(projectPath, ".ispac"), null));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Fail_LoadFromIspac_FileNotFound()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.dtproj";
            var proj = new Project();

            // Execute
            var exception = Record.Exception(() => proj.LoadFromIspac(Path.ChangeExtension(Path.Combine(_workingFolder, projectName), ".ispac"), null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<FileNotFoundException>(exception);
        }

        [Fact]
        public void Fail_LoadFromIspac_InvalidExtension()
        {
            // Setup
            var projectName = $"proj_{Fakes.RandomString()}.xyz";
            var projectPath = Path.Combine(_workingFolder, projectName);
            File.Create(projectPath).Close();
            var proj = new Project();

            // Execute
            var exception = Record.Exception(() => proj.LoadFromIspac(projectPath, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidExtensionException>(exception);
        }

        [Fact]
        public void Pass_OriginalParameters()
        {
            // Setup
            var manifestMock = new Mock<IProjectManifest>();

            var manifestParameters = new[]
            {
                CreateParameter($"Project::{Fakes.RandomString()}", Fakes.RandomString(), Fakes.RandomBool(), Fakes.RandomEnum<ParameterSource>()),
                CreateParameter($"Project::{Fakes.RandomString()}", Fakes.RandomString(), Fakes.RandomBool(), Fakes.RandomEnum<ParameterSource>()),
            }.ToDictionary(p => p.Name, p => p);

            manifestMock.Setup(m => m.Parameters).Returns(manifestParameters);
            var manifest = manifestMock.Object;

            var projectParamsMock = new Mock<IProjectFile>();
            var projectParameters = new[]
            {
                CreateParameter($"Project::{Fakes.RandomString()}", Fakes.RandomString(), Fakes.RandomBool(), Fakes.RandomEnum<ParameterSource>()),
                CreateParameter($"Project::{Fakes.RandomString()}", Fakes.RandomString(), Fakes.RandomBool(), Fakes.RandomEnum<ParameterSource>()),
            }.ToDictionary(p => p.Name, p => p);

            projectParamsMock.Setup(m => m.Parameters).Returns(projectParameters);
            var projectParams = projectParamsMock.Object;

            // Execute
            var project = new Project();
            typeof(Project).GetField("_projectManifest", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, manifest);
            typeof(Project).GetField("_projectParams", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, projectParams);
            typeof(Project).GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, true);

            // Assert
            Assert.Equal(4, project.Parameters.Count);
            foreach (var parameter in projectParameters)
            {
                Assert.True(project.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(parameter.Value.Value, project.Parameters[parameter.Key].Value);
                Assert.Equal(parameter.Value.ParameterDataType, project.Parameters[parameter.Key].ParameterDataType);
                Assert.Equal(parameter.Value.Sensitive, project.Parameters[parameter.Key].Sensitive);
                Assert.Equal(parameter.Value.Source, project.Parameters[parameter.Key].Source);
            }
            foreach (var parameter in manifestParameters)
            {
                Assert.True(project.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(parameter.Value.Value, project.Parameters[parameter.Key].Value);
                Assert.Equal(parameter.Value.ParameterDataType, project.Parameters[parameter.Key].ParameterDataType);
                Assert.Equal(parameter.Value.Sensitive, project.Parameters[parameter.Key].Sensitive);
                Assert.Equal(parameter.Value.Source, project.Parameters[parameter.Key].Source);
            }
        }

        [Fact]
        public void Pass_UpdateParameters()
        {
            // Setup
            var manifestMock = new Mock<IProjectManifest>();

            var manifestParameters = new[]
            {
                CreateParameter($"Project::{Fakes.RandomString()}", Fakes.RandomString(), Fakes.RandomBool(), ParameterSource.Original),
                CreateParameter($"Project::{Fakes.RandomString()}", Fakes.RandomString(), Fakes.RandomBool(), ParameterSource.Original),
            }.ToDictionary(p => p.Name, p => p);

            manifestMock.Setup(m => m.Parameters).Returns(manifestParameters);
            var manifest = manifestMock.Object;

            var projectParamsMock = new Mock<IProjectFile>();
            var projectParameters = new[]
            {
                CreateParameter($"Project::{Fakes.RandomString()}", Fakes.RandomString(), Fakes.RandomBool(), ParameterSource.Original),
                CreateParameter($"Project::{Fakes.RandomString()}", Fakes.RandomString(), Fakes.RandomBool(), ParameterSource.Original),
            }.ToDictionary(p => p.Name, p => p);

            projectParamsMock.Setup(m => m.Parameters).Returns(projectParameters);
            var projectParams = projectParamsMock.Object;

            // Execute
            var project = new Project();
            typeof(Project).GetField("_projectManifest", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, manifest);
            typeof(Project).GetField("_projectParams", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, projectParams);
            typeof(Project).GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, true);

            // Assert
            Assert.Equal(4, project.Parameters.Count);
            foreach (var parameter in projectParameters)
            {
                var newValue = Fakes.RandomString();
                project.UpdateParameter(parameter.Value.Name, newValue, ParameterSource.Manual);

                Assert.True(project.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(newValue, project.Parameters[parameter.Key].Value);
                Assert.Equal(ParameterSource.Manual, project.Parameters[parameter.Key].Source);
            }
            foreach (var parameter in manifestParameters)
            {
                var newValue = Fakes.RandomString();
                project.UpdateParameter(parameter.Value.Name, newValue, ParameterSource.Manual);

                Assert.True(project.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(newValue, project.Parameters[parameter.Key].Value);
                Assert.Equal(ParameterSource.Manual, project.Parameters[parameter.Key].Source);
            }

        }

        [Fact]
        public void Pass_OriginalVersionInfo()
        {
            // Setup
            var manifestMock = new Mock<IProjectManifest>();

            manifestMock.SetupAllProperties();
            var manifest = manifestMock.Object;
            manifest.VersionMajor = Fakes.RandomInt(0, 100);
            manifest.VersionMinor = Fakes.RandomInt(0, 100);
            manifest.VersionBuild = Fakes.RandomInt(0, 100);
            manifest.VersionComments = Fakes.RandomString();
            manifest.Description = Fakes.RandomString();
            manifest.ProtectionLevel  = Fakes.RandomEnum<ProtectionLevel>();
            
            // Execute
            var project = new Project();
            typeof(Project).GetField("_projectManifest", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, manifest);
            typeof(Project).GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, true);

            // Assert
            Assert.Equal(manifest.VersionMajor, project.VersionMajor);
            Assert.Equal(manifest.VersionMinor, project.VersionMinor);
            Assert.Equal(manifest.VersionBuild, project.VersionBuild);
            Assert.Equal(manifest.VersionComments, project.VersionComments);
            Assert.Equal(manifest.Description, project.Description);
            Assert.Equal(manifest.ProtectionLevel, project.ProtectionLevel);

        }

        [Fact]
        public void Pass_UpdateVersionInfo()
        {
            // Setup
            var manifestMock = new Mock<IProjectManifest>();

            var protectionLevel = Fakes.RandomEnum<ProtectionLevel>();
            manifestMock.SetupGet(m => m.ProtectionLevel).Returns(protectionLevel);

            manifestMock.SetupAllProperties();
            var manifest = manifestMock.Object;

            var project = new Project();
            typeof(Project).GetField("_projectManifest", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, manifest);
            typeof(Project).GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, true);

            // Execute
            project.VersionMajor = Fakes.RandomInt(0, 100);
            project.VersionMinor = Fakes.RandomInt(0, 100);
            project.VersionBuild = Fakes.RandomInt(0, 100);
            project.VersionComments = Fakes.RandomString();
            project.Description = Fakes.RandomString();

            // Assert
            Assert.Equal(project.VersionMajor, manifest.VersionMajor);
            Assert.Equal(project.VersionMinor, manifest.VersionMinor);
            Assert.Equal(project.VersionBuild, manifest.VersionBuild);
            Assert.Equal(project.VersionComments, manifest.VersionComments);
            Assert.Equal(project.Description, manifest.Description);
            Assert.Equal(project.ProtectionLevel, manifest.ProtectionLevel);
        }


        private IParameter CreateParameter(string name, string value, bool sensitive, ParameterSource source)
        {
            var parameterMock = new Mock<IParameter>();
            parameterMock.Setup(p => p.Name).Returns(name);
            parameterMock.Setup(p => p.Sensitive).Returns(sensitive);
            parameterMock.Setup(p => p.Value).Returns(value);
            parameterMock.Setup(p => p.Source).Returns(source);
            parameterMock.Setup(p=>p.SetValue(It.IsAny<string>(), It.IsAny<ParameterSource>())).Callback(
                (string newValue, ParameterSource newSource) =>
                {
                    parameterMock.Setup(p => p.Value).Returns(newValue);
                    parameterMock.Setup(p => p.Source).Returns(newSource);

                });

            return parameterMock.Object;
        }

        internal void CreateDtprojFiles(string projectName, string configurationName)
        {
            var packages = new[] {$"p_{Fakes.RandomString()}.dtsx", $"p_{Fakes.RandomString()}.dtsx"};
            var connections = new[] { $"c_{Fakes.RandomString()}.conmgr", $"c_{Fakes.RandomString()}.conmgr" };


            var paramName = Fakes.RandomString();

            var projectParamsXml = XmlGenerators.ProjectParamsFile(new List<ParameterSetupData>(){
                {new ParameterSetupData
                {
                    Value = Fakes.RandomString(),
                    Name = paramName,
                    DataType = DataType.String,
                    Sensitive = false
                } }
            });

            var projectManifestXml = XmlGenerators.ProjectManifestFile(ProtectionLevel.DontSaveSensitive, 1, 2, Fakes.RandomString(), 3, "Descr", packages, connections,
                new[]
                {
                    new ParameterSetupData()
                    {
                        Value = Fakes.RandomString(),
                        DataType = DataType.String,
                        Name = Fakes.RandomString(),
                        Sensitive = false
                    }, 
                });
            var configurationXml = XmlGenerators.ConfigurationFile(configurationName, new Dictionary<string, string>()
            {
                {
                    $"Project::{paramName}", Fakes.RandomString() 
                }
            });
            var configurationsXmlDoc = new XmlDocument();
            configurationsXmlDoc.LoadXml(configurationXml);

            var userConfigurationXml = XmlGenerators.UserConfigurationFile(configurationName, new Dictionary<string, string>()
            {
                {
                    $"Project::{paramName}", Fakes.RandomString()
                }
            });
            var dtprojXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <Project xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                  <DeploymentModel>Project</DeploymentModel>
                  <DeploymentModelSpecificContent>
                    <Manifest>
                      {projectManifestXml}
                    </Manifest>
                  </DeploymentModelSpecificContent>
                  <Configurations>
                      {configurationsXmlDoc.SelectSingleNode("//Configuration", configurationsXmlDoc.GetNameSpaceManager())?.OuterXml}
                  </Configurations>
                </Project>";

            var dtproj = projectName;
            File.WriteAllText(Path.Combine(_workingFolder, dtproj), dtprojXml);
            File.WriteAllText(Path.Combine(_workingFolder, "Project.params"), projectParamsXml);
            File.WriteAllText($"{Path.Combine(_workingFolder, dtproj)}.user", userConfigurationXml);
            foreach (var package in packages)
            {
                var packageXml = XmlGenerators.PackageFile(Fakes.RandomString(), (int)ProtectionLevel.EncryptSensitiveWithPassword, Fakes.RandomString());
                File.WriteAllText($"{Path.Combine(_workingFolder, package)}", packageXml);
            }

            foreach (var connection in connections)
            {
                var projectConnectionsXml = XmlGenerators.ProjectConnectionsFile();
                File.WriteAllText($"{Path.Combine(_workingFolder, connection)}", projectConnectionsXml);
            }
        }

        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
        }
    }
}