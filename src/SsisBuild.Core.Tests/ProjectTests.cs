using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Moq;
using SsisBuild.Core.Helpers;
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
            var projectName = $"proj_{Helpers.RandomString(10)}.dtproj";
            CreateDtprojFiles(projectName);
            var proj = new Project();

            proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), "Anything", "123");
        }

        [Fact]
        public void Fail_LoadFromDtproj_FileNotFound()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.dtproj";

            var proj = new Project();
            var exception = Record.Exception(() => proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), "Anything", "123"));
            Assert.NotNull(exception);
            Assert.IsType<FileNotFoundException>(exception);
        }

        [Fact]
        public void Fail_LoadFromDtproj_BadExtension()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.xyz";
            File.Create(Path.Combine(_workingFolder, projectName)).Close();

            var proj = new Project();
            var exception = Record.Exception(() => proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), "Anything", "123"));
            Assert.NotNull(exception);
            Assert.IsType<InvalidExtensionException>(exception);
        }


        [Fact]
        public void Fail_LoadFromDtproj_NoManifest()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.dtproj";
            CreateDtprojFiles(projectName);
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(Path.Combine(_workingFolder, projectName));
            var manifestNode = xmlDoc.SelectSingleNode("//SSIS:Project", xmlDoc.GetNameSpaceManager());
            var parent = manifestNode?.ParentNode;
            parent?.RemoveChild(manifestNode);
            xmlDoc.Save(Path.Combine(_workingFolder, projectName));

            var proj = new Project();
            var exception = Record.Exception(() => proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), "Anything", "123"));
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Manifest"));
        }

        [Fact]
        public void Fail_UpdateParameter_NotLoaded()
        {
            var proj = new Project();
            var exception = Record.Exception(() => proj.UpdateParameter(Helpers.RandomString(10), Helpers.RandomString(10), ParameterSource.Configuration));
            Assert.NotNull(exception);
            Assert.IsType<ProjectNotInitializedException>(exception);
        }

        [Fact]
        public void Fail_Dtproj_InvalidDeploymentModel()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.dtproj";
            CreateDtprojFiles(projectName);
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(Path.Combine(_workingFolder, projectName));
            var deploymentModelNode = xmlDoc.SelectSingleNode("/Project/DeploymentModel", xmlDoc.GetNameSpaceManager());
            if (deploymentModelNode != null)
                deploymentModelNode.InnerText = Helpers.RandomString(20);

            xmlDoc.Save(Path.Combine(_workingFolder, projectName));

            var proj = new Project();
            var exception = Record.Exception(() => proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), "Anything", "123"));
            Assert.NotNull(exception);
            Assert.IsType<InvalidDeploymentModelException>(exception);
        }

        [Fact]
        public void Pass_Save()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.dtproj";
            CreateDtprojFiles(projectName);
            var proj = new Project();

            proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), "Anything", "123");
            var exception = Record.Exception(() => proj.Save(Path.ChangeExtension(Path.Combine(_workingFolder, projectName), ".ispac")));
            Assert.Null(exception);
        }

        [Fact]
        public void Fail_Save_NotInitialized_ToFile()
        {
            var ispacName = $"proj_{Helpers.RandomString(10)}.ispac";
            var proj = new Project();

            var exception = Record.Exception(() => proj.Save(Path.Combine(_workingFolder, ispacName)));
            Assert.IsType<ProjectNotInitializedException>(exception);
        }

        [Fact]
        public void Fail_Save_NotInitialized_ToStream()
        {
            var ispacName = $"proj_{Helpers.RandomString(10)}.ispac";
            var proj = new Project();

            Exception exception;
            using (var stream = File.OpenWrite(Path.Combine(_workingFolder, ispacName)))
            {
                exception = Record.Exception(() => proj.Save(stream, ProtectionLevel.DontSaveSensitive, null));
            }
            Assert.IsType<ProjectNotInitializedException>(exception);
        }

        [Fact]
        public void Fail_Save_InvalidExtension()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.dtproj";
            CreateDtprojFiles(projectName);
            var proj = new Project();

            proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), "Anything", "123");
            var exception = Record.Exception(() => proj.Save(Path.ChangeExtension(Path.Combine(_workingFolder, projectName), ".xyz")));
            Assert.NotNull(exception);
            Assert.IsType<InvalidExtensionException>(exception);
        }

        [Fact]
        public void Pass_LoadFromIspac()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.dtproj";
            CreateDtprojFiles(projectName);
            var proj = new Project();

            proj.LoadFromDtproj(Path.Combine(_workingFolder, projectName), "Anything", "123");

            proj.Save(Path.ChangeExtension(Path.Combine(_workingFolder, projectName), ".ispac"));

            var newProj = new Project();
            var exception = Record.Exception(() => newProj.LoadFromIspac(Path.ChangeExtension(Path.Combine(_workingFolder, projectName), ".ispac"), null));
            Assert.Null(exception);
        }

        [Fact]
        public void Fail_LoadFromIspac_FileNotFound()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.dtproj";
            var proj = new Project();
            var exception = Record.Exception(() => proj.LoadFromIspac(Path.ChangeExtension(Path.Combine(_workingFolder, projectName), ".ispac"), null));
            Assert.NotNull(exception);
            Assert.IsType<FileNotFoundException>(exception);
        }

        [Fact]
        public void Fail_LoadFromIspac_InvalidExtension()
        {
            var projectName = $"proj_{Helpers.RandomString(10)}.xyz";
            File.Create(Path.Combine(_workingFolder, projectName)).Close();
            var proj = new Project();
            var exception = Record.Exception(() => proj.LoadFromIspac(Path.Combine(_workingFolder, projectName), null));
            Assert.NotNull(exception);
            Assert.IsType<InvalidExtensionException>(exception);
        }

        [Fact]
        public void Pass_OriginalParameters()
        {
            var manifestMock = new Mock<IProjectManifest>();

            var manifestParameters = new[]
            {
                CreateParameter("Project::CN_xyz_1", Helpers.RandomString(20), false, ParameterSource.Original),
                CreateParameter("Project::CN_xyz_2", Helpers.RandomString(20), false, ParameterSource.Original),
            }.ToDictionary(p => p.Name, p => p);

            manifestMock.Setup(m => m.Parameters).Returns(manifestParameters);
            var manifest = manifestMock.Object;

            var projectParamsMock = new Mock<IProjectFile>();
            var projectParameters = new[]
            {
                CreateParameter("Project::Parameter1", Helpers.RandomString(20), false, ParameterSource.Original),
                CreateParameter("Project::Parameter2", Helpers.RandomString(20), false, ParameterSource.Original),
            }.ToDictionary(p => p.Name, p => p);

            projectParamsMock.Setup(m => m.Parameters).Returns(projectParameters);
            var projectParams = projectParamsMock.Object;

            var project = new Project();
            typeof(Project).GetField("_projectManifest", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, manifest);
            typeof(Project).GetField("_projectParams", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, projectParams);
            typeof(Project).GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, true);

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
            var manifestMock = new Mock<IProjectManifest>();

            var manifestParameters = new[]
            {
                CreateParameter("Project::CN_xyz_1", Helpers.RandomString(20), false, ParameterSource.Original),
                CreateParameter("Project::CN_xyz_2", Helpers.RandomString(20), false, ParameterSource.Original),
            }.ToDictionary(p => p.Name, p => p);

            manifestMock.Setup(m => m.Parameters).Returns(manifestParameters);
            var manifest = manifestMock.Object;

            var projectParamsMock = new Mock<IProjectFile>();
            var projectParameters = new[]
            {
                CreateParameter("Project::Parameter1", Helpers.RandomString(20), false, ParameterSource.Original),
                CreateParameter("Project::Parameter2", Helpers.RandomString(20), false, ParameterSource.Original),
            }.ToDictionary(p => p.Name, p => p);

            projectParamsMock.Setup(m => m.Parameters).Returns(projectParameters);
            var projectParams = projectParamsMock.Object;

            var project = new Project();
            typeof(Project).GetField("_projectManifest", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, manifest);
            typeof(Project).GetField("_projectParams", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, projectParams);
            typeof(Project).GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, true);

            Assert.Equal(4, project.Parameters.Count);
            foreach (var parameter in projectParameters)
            {
                var newValue = Helpers.RandomString(10);
                project.UpdateParameter(parameter.Value.Name, newValue, ParameterSource.Manual);

                Assert.True(project.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(newValue, project.Parameters[parameter.Key].Value);
                Assert.Equal(ParameterSource.Manual, project.Parameters[parameter.Key].Source);
            }
            foreach (var parameter in manifestParameters)
            {
                var newValue = Helpers.RandomString(10);
                project.UpdateParameter(parameter.Value.Name, newValue, ParameterSource.Manual);

                Assert.True(project.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(newValue, project.Parameters[parameter.Key].Value);
                Assert.Equal(ParameterSource.Manual, project.Parameters[parameter.Key].Source);
            }

        }

        [Fact]
        public void Pass_OriginalVersionInfo()
        {
            var manifestMock = new Mock<IProjectManifest>();

            var rnd = new Random(DateTime.Now.Millisecond);

            manifestMock.SetupAllProperties();
            var manifest = manifestMock.Object;
            manifest.VersionMajor = rnd.Next();
            manifest.VersionMinor = rnd.Next();
            manifest.VersionBuild = rnd.Next();
            manifest.VersionComments = Helpers.RandomString(100);
            manifest.Description = Helpers.RandomString(100);
            manifest.ProtectionLevel  = ProtectionLevel.DontSaveSensitive;
            ;

            var project = new Project();
            typeof(Project).GetField("_projectManifest", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, manifest);
            typeof(Project).GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, true);

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
            var proj = new Project();
            var manifestMock = new Mock<IProjectManifest>();

            var protectionLevel = ProtectionLevel.DontSaveSensitive;
            manifestMock.SetupGet(m => m.ProtectionLevel).Returns(protectionLevel);

            var rnd = new Random(DateTime.Now.Millisecond);

            manifestMock.SetupAllProperties();
            var manifest = manifestMock.Object;

            var project = new Project();
            typeof(Project).GetField("_projectManifest", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, manifest);
            typeof(Project).GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(project, true);

            project.VersionMajor = rnd.Next();
            project.VersionMinor = rnd.Next();
            project.VersionBuild = rnd.Next();
            project.VersionComments = Helpers.RandomString(100);
            project.Description = Helpers.RandomString(100);

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

        internal void CreateDtprojFiles(string projectName)
        {
            var packages = new string[] {$"p_{Helpers.RandomString(20)}.dtsx", $"p_{Helpers.RandomString(20)}.dtsx"};
            var connections = new string[] { $"c_{Helpers.RandomString(20)}.conmgr", $"c_{Helpers.RandomString(20)}.conmgr" };


            var paramName = Helpers.RandomString(10);

            var projectParamsXml = ProjectParamsTests.CreateXml(new List<ProjectParamsTests.ParameterSetupData>(){
                {new ProjectParamsTests.ParameterSetupData()
                {
                    Value = Helpers.RandomString(10),
                    Name = paramName,
                    DataType = DataType.String,
                    Sensitive = false
                } }
            });

            var projectManifestXml = ProjectManifestTests.CreateXml(ProtectionLevel.DontSaveSensitive, 1, 2, Helpers.RandomString(20), 3, "Descr", packages, connections,
                new ProjectManifestTests.ParameterSetupData[]
                {
                    new ProjectManifestTests.ParameterSetupData()
                    {
                        Value = Helpers.RandomString(20),
                        DataType = DataType.String,
                        Name = Helpers.RandomString(20),
                        Sensitive = false
                    }, 
                });
            var configurationXml = ConfigurationTests.GetXml("Anything", new Dictionary<string, string>()
            {
                {
                    $"Project::{paramName}", Helpers.RandomString(10) 
                }
            });
            var configurationsXmlDoc = new XmlDocument();
            configurationsXmlDoc.LoadXml(configurationXml);

            var userConfigurationXml = UserConfigurationTests.GetXml("Anything", new Dictionary<string, string>()
            {
                {
                    $"Project::{paramName}", Helpers.RandomString(10)
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
                var packageXml = PackageTests.CreateXml("123", (int)ProtectionLevel.EncryptSensitiveWithPassword, "345");
                File.WriteAllText($"{Path.Combine(_workingFolder, package)}", packageXml);
            }

            foreach (var connection in connections)
            {
                var projectConnectionsXml = CreateProjectConnectionsXml();
                File.WriteAllText($"{Path.Combine(_workingFolder, connection)}", projectConnectionsXml);
            }
        }

        private static string CreateProjectConnectionsXml()
        {
            return $@"<?xml version=""1.0""?>
                <DTS:ConnectionManager xmlns:DTS=""www.microsoft.com/SqlServer/Dts""
                  DTS:ObjectName=""{Helpers.RandomString(20)}""
                  DTS:DTSID=""{Guid.NewGuid():B}""
                  DTS:CreationName=""{Helpers.RandomString(100)}"">
                  <DTS:ObjectData>
                  </DTS:ObjectData>
                </DTS:ConnectionManager>";
        }

        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
        }
    }
}