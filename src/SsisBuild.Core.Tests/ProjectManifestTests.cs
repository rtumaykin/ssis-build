using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SsisBuild.Core.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ProjectManifestTests : IDisposable
    {
        private string _workingFolder;

        public class ParameterSetupData
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool Sensitive { get; set; }
            public DataType DataType { get; set; }
        }

        public ProjectManifestTests()
        {
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingFolder);
        }



        [Theory, MemberData(nameof(ParameterData))]
        public void Pass_New(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            var xml = CreateXml(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectManifest = new ProjectManifest();
            projectManifest.Initialize(path, null);
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
        public void Pass_SetProtectionLevel(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            var xml = CreateXml(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectManifest = new ProjectManifest();
            projectManifest.Initialize(path, null);
            Assert.NotNull(projectManifest);

            var desiredProtectionLevel = projectManifest.ProtectionLevel == ProtectionLevel.DontSaveSensitive
                ? ProtectionLevel.ServerStorage
                : ProtectionLevel.DontSaveSensitive;

            string savedXml;

            using (var stream = new MemoryStream())
            {
                projectManifest.Save(stream, desiredProtectionLevel, Helpers.RandomString(20));
                stream.Flush();
                stream.Position = 0;

                var sr = new StreamReader(stream);
                savedXml = sr.ReadToEnd();
            }

            var testXmlDoc = new XmlDocument();
            testXmlDoc.LoadXml(savedXml);
            var projectProtectionLevel = testXmlDoc.DocumentElement?.Attributes["SSIS:ProtectionLevel"]?.Value;
            ProtectionLevel testProtectionLevel;
            Assert.True(Enum.TryParse(projectProtectionLevel, out testProtectionLevel));
            Assert.Equal(desiredProtectionLevel, testProtectionLevel);

            var packageProtectionLevelNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"ProtectionLevel\"]", testXmlDoc.GetNameSpaceManager());
            Assert.NotNull(packageProtectionLevelNodes);

            foreach (XmlElement packageProtectionElement in packageProtectionLevelNodes)
            {
                Assert.Equal(desiredProtectionLevel, (ProtectionLevel)int.Parse(packageProtectionElement.InnerText));
            }

            Assert.Equal(desiredProtectionLevel, projectManifest.ProtectionLevel);
        }

        [Theory, MemberData(nameof(ParameterData))]
        public void Pass_SetVersion(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            var xml = CreateXml(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);
            var projectManifest = new ProjectManifest();
            projectManifest.Initialize(path, null);
            Assert.NotNull(projectManifest);

            var newVersionMajor = projectManifest.VersionMajor * 2;
            var newVersionMinor = projectManifest.VersionMinor * 2;
            var newVersionBuild = projectManifest.VersionBuild * 2;
            var newVersionComments = Helpers.RandomString(20);
            var newDescription = Helpers.RandomString(20);

            projectManifest.VersionBuild = newVersionBuild;
            projectManifest.VersionMajor = newVersionMajor;
            projectManifest.VersionMinor = newVersionMinor;
            projectManifest.VersionComments = newVersionComments;
            projectManifest.Description = newDescription;

            string savedXml;

            using (var stream = new MemoryStream())
            {
                projectManifest.Save(stream, ProtectionLevel.DontSaveSensitive, Helpers.RandomString(20));
                stream.Flush();
                stream.Position = 0;

                var sr = new StreamReader(stream);
                savedXml = sr.ReadToEnd();
            }
            var testXmlDoc = new XmlDocument();
            testXmlDoc.LoadXml(savedXml);

            var versionMajorNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMajor\"]", testXmlDoc.GetNameSpaceManager());
            Assert.NotNull(versionMajorNodes);
            foreach (XmlElement versionMajorNode in versionMajorNodes)
            {
                Assert.Equal(newVersionMajor, int.Parse(versionMajorNode.InnerText));
            }

            var versionMinorNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionMinor\"]", testXmlDoc.GetNameSpaceManager());
            Assert.NotNull(versionMinorNodes);
            foreach (XmlElement versionMinorNode in versionMinorNodes)
            {
                Assert.Equal(newVersionMinor, int.Parse(versionMinorNode.InnerText));
            }

            var versionBuildNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionBuild\"]", testXmlDoc.GetNameSpaceManager());
            Assert.NotNull(versionBuildNodes);
            foreach (XmlElement versionBuildNode in versionBuildNodes)
            {
                Assert.Equal(newVersionBuild, int.Parse(versionBuildNode.InnerText));
            }

            var versionCommentsNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"VersionComments\"]", testXmlDoc.GetNameSpaceManager());
            Assert.NotNull(versionCommentsNodes);
            foreach (XmlElement versionCommentsNode in versionCommentsNodes)
            {
                Assert.Equal(newVersionComments, versionCommentsNode.InnerText);
            }

            var descriptionNodes = testXmlDoc.SelectNodes("/SSIS:Project/SSIS:Properties/SSIS:Property[@SSIS:Name = \"Description\"]", testXmlDoc.GetNameSpaceManager());
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
            var projectManifestXmlDoc = new XmlDocument();
            var xml = CreateXml(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            projectManifestXmlDoc.LoadXml(xml);
            projectManifestXmlDoc.DocumentElement.Attributes["SSIS:ProtectionLevel"].Value = string.Empty;

            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, projectManifestXmlDoc.OuterXml);
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("SSIS:ProtectionLevel"));
        }

        [Theory, MemberData(nameof(ParameterData))]
        public void Fail_InvalidProtectionLevelString(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            var projectManifestXmlDoc = new XmlDocument();
            var xml = CreateXml(protectionLevel, versionMajor, versionMinor, versionComments, versionBuild, description, packages, connectionManagers, parameters);
            projectManifestXmlDoc.LoadXml(xml);
            projectManifestXmlDoc.DocumentElement.Attributes["SSIS:ProtectionLevel"].Value = Helpers.RandomString(20);

            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, projectManifestXmlDoc.OuterXml);
            var projectManifest = new ProjectManifest();
            var exception = Record.Exception(() => projectManifest.Initialize(path, null));
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.True(exception.Message.Contains("Invalid Protection Level"));
        }

        private static string CreateXml(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description, string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            return $@"<SSIS:Project SSIS:ProtectionLevel=""{protectionLevel:G}"" xmlns:SSIS=""www.microsoft.com/SqlServer/SSIS"">
	            <SSIS:Properties>
	              <SSIS:Property SSIS:Name=""ID"">{Guid.NewGuid():B}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""Name"">{Helpers.RandomString(20)}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""VersionMajor"">{versionMajor}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""VersionMinor"">{versionMinor}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""VersionBuild"">{versionBuild}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""VersionComments"">{versionComments}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""CreationDate""></SSIS:Property>
	              <SSIS:Property SSIS:Name=""CreatorName"">{Helpers.RandomString(20)}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""CreatorComputerName"">{Helpers.RandomString(20)}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""Description"">{description}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""FormatVersion"">1</SSIS:Property>
	            </SSIS:Properties>
	            <SSIS:Packages>
	              {string.Join("", packages.Select(CreatePackageXml))}
	            </SSIS:Packages>
	            <SSIS:ConnectionManagers>
	              {string.Join("", connectionManagers.Select(CreateConnManagerXml))}
	            </SSIS:ConnectionManagers>
	            <SSIS:DeploymentInfo>
	              <SSIS:ProjectConnectionParameters>
		            {string.Join("", parameters.Select(p=>CreateParameterXml(p.Name, p.Value, p.Sensitive, p.DataType)))}
	              </SSIS:ProjectConnectionParameters>
                  <SSIS:PackageInfo>
                    {string.Join("", packages.Select(p=> CreatePackageMetadataXml(p, parameters, versionMajor, versionMinor, versionBuild, versionComments, protectionLevel)))}
                  </SSIS:PackageInfo>
                </SSIS:DeploymentInfo>		  
            </SSIS:Project>";
        }

        private static string CreatePackageMetadataXml(string packageName, ParameterSetupData[] parameters, int versionMajor, int versionMinor, int versionBuild, string versionComments, ProtectionLevel protectionLevel)
        {
            return $@"<SSIS:PackageMetaData SSIS:Name=""{packageName}.dtsx"">
                      <SSIS:Properties>
		                <SSIS:Property SSIS:Name=""ID"">{Guid.NewGuid():B}</SSIS:Property>
		                <SSIS:Property SSIS:Name=""Name"">{packageName}</SSIS:Property>
		                <SSIS:Property SSIS:Name=""VersionMajor"">{versionMajor}</SSIS:Property>
		                <SSIS:Property SSIS:Name=""VersionMinor"">{versionMinor}</SSIS:Property>
		                <SSIS:Property SSIS:Name=""VersionBuild"">{versionBuild}</SSIS:Property>
		                <SSIS:Property SSIS:Name=""VersionComments"">{versionComments}</SSIS:Property>
		                <SSIS:Property SSIS:Name=""VersionGUID"">{Guid.NewGuid():B}</SSIS:Property>
		                <SSIS:Property SSIS:Name=""PackageFormatVersion"">6</SSIS:Property>
		                <SSIS:Property SSIS:Name=""Description"">
		                </SSIS:Property>
		                <SSIS:Property SSIS:Name=""ProtectionLevel"">{protectionLevel:D}</SSIS:Property>
	                  </SSIS:Properties>
	                  <SSIS:Parameters>
    		            {string.Join("", parameters.Select(p => CreateParameterXml(p.Name, p.Value, p.Sensitive, p.DataType)))}
                      </SSIS:Parameters>
	                </SSIS:PackageMetaData>";
        }

        private static string CreatePackageXml(string name)
        {
            return $@"<SSIS:Package SSIS:Name=""{name}"" SSIS:EntryPoint=""1"" />";
        }

        private static string CreateConnManagerXml(string name)
        {
            return $@"<SSIS:ConnectionManager SSIS:Name=""{name}"" />";
        }

        private static string CreateParameterXml(string name, string value, bool sensitive, DataType dataType)
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
            var protectionLevels = new[] {ProtectionLevel.DontSaveSensitive, ProtectionLevel.EncryptSensitiveWithPassword, ProtectionLevel.EncryptAllWithPassword};

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

                var packages = new List<string>();
                var packageNum = rnd.Next(0, 20);
                for (var cnt1 = 0; cnt1 < packageNum; cnt1++)
                {
                    packages.Add(Helpers.RandomString(30));
                }

                var connections = new List<string>();
                var connectionsNum = rnd.Next(0, 20);
                for (var cnt1 = 0; cnt1 < connectionsNum; cnt1++)
                {
                    connections.Add(Helpers.RandomString(30));
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
                    protectionLevels[rnd.Next(0, 299) / 100],
                    rnd.Next(0, 100),
                    rnd.Next(0, 100),
                    Helpers.RandomString(100),
                    rnd.Next(0, 100),
                    Helpers.RandomString(100),
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