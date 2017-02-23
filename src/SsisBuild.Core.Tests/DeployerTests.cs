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
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Moq;
using SsisBuild.Core.Deployer;
using SsisBuild.Core.ProjectManagement;
using SsisBuild.Logger;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class DeployerTests
    {

        private readonly Mock<IProject> _projectMock;
        private readonly Mock<ICatalogTools> _catalogToolsMock;
        private readonly Mock<IDeployArguments> _deployArgumentsMock;
        private readonly Mock<ILogger> _loggerMock;

        public DeployerTests()
        {
            _projectMock = new Mock<IProject>();
            _catalogToolsMock = new Mock<ICatalogTools>();
            _deployArgumentsMock = new Mock<IDeployArguments>();
            _loggerMock = new Mock<ILogger>();
        }

        [Fact]
        public void Pass_Deploy()
        {
            // Setup
            var parameters = GenerateRandomParameters();
            _projectMock.SetupAllProperties();
            _projectMock.Setup(p => p.Parameters).Returns(new ReadOnlyDictionary<string, IParameter>(parameters));

            _deployArgumentsMock.Setup(d => d.Catalog).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.DeploymentFilePath).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.Folder).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.ProjectName).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.ProjectPassword).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.EraseSensitiveInfo).Returns(Fakes.RandomBool());
            _deployArgumentsMock.Setup(d => d.ServerInstance).Returns(Fakes.RandomString());

            string passedCatalog = null;
            string passedServerInstance = null;
            string passedFolder = null;
            string passedProjectName = null;
            bool? passedEraseSensitiveInfo = null;
            SensitiveParameter[] passedParameters = null;

            _catalogToolsMock.Setup(c=>c.DeployProject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, SensitiveParameter>>(), It.IsAny<MemoryStream>()))
                .Callback(
                    (string connectionString, string folderName, string projectName, bool eraseSensitiveInfo, IDictionary<string, SensitiveParameter> parametersToDeploy,
                        MemoryStream projectStream) =>
                    {
                        var sb = new SqlConnectionStringBuilder(connectionString);
                        passedCatalog = sb.InitialCatalog;
                        passedServerInstance = sb.DataSource;

                        passedFolder = folderName;
                        passedProjectName = projectName;
                        passedEraseSensitiveInfo = eraseSensitiveInfo;
                        passedParameters = parametersToDeploy.Values.ToArray();
                    })
                .Verifiable();

            _loggerMock.Setup(l=>l.LogMessage(It.IsAny<string>())).Verifiable();
            var deployer = new Deployer.Deployer(_loggerMock.Object, _projectMock.Object, _catalogToolsMock.Object);
            
            // Execute
            deployer.Deploy(_deployArgumentsMock.Object);

            // Assert
            _loggerMock.Verify(m=>m.LogMessage(It.IsAny<string>()));
            Assert.Equal(_deployArgumentsMock.Object.Catalog, passedCatalog);
            Assert.Equal(_deployArgumentsMock.Object.ServerInstance, passedServerInstance);
            Assert.Equal(_deployArgumentsMock.Object.Folder, passedFolder);
            Assert.Equal(_deployArgumentsMock.Object.ProjectName, passedProjectName);
            Assert.NotNull(passedEraseSensitiveInfo);
            Assert.Equal(_deployArgumentsMock.Object.EraseSensitiveInfo, passedEraseSensitiveInfo.Value);

            foreach (var parameter in parameters.Where(p=>p.Value.Sensitive))
            {
                Assert.True(passedParameters.Any(pp => pp.Name == parameter.Key && pp.Value == parameter.Value.Value && pp.DataType == parameter.Value.ParameterDataType));
            }
        }

        [Fact]
        public void Pass_Deployer()
        {
            // Setup
            
            // Execute
            var deployer = new Deployer.Deployer();
            
            // Assert
            Assert.NotNull(deployer);
        }

        [Fact]
        public void Fail_Deploy()
        {
            // Setup
            var parameters = GenerateRandomParameters();
            _projectMock.SetupAllProperties();
            _projectMock.Setup(p => p.Parameters).Returns(new ReadOnlyDictionary<string, IParameter>(parameters));

            _deployArgumentsMock.Setup(d => d.Catalog).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.DeploymentFilePath).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.Folder).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.ProjectName).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.ProjectPassword).Returns(Fakes.RandomString());
            _deployArgumentsMock.Setup(d => d.EraseSensitiveInfo).Returns(Fakes.RandomBool());
            _deployArgumentsMock.Setup(d => d.ServerInstance).Returns(Fakes.RandomString());

            _catalogToolsMock.Setup(c => c.DeployProject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, SensitiveParameter>>(), It.IsAny<MemoryStream>()))
                .Throws(new Exception("TEST"));

            _loggerMock.Setup(l => l.LogMessage(It.IsAny<string>())).Verifiable();

            // Execute
            var deployer = new Deployer.Deployer(_loggerMock.Object, _projectMock.Object, _catalogToolsMock.Object);
            var exception = Record.Exception(() => deployer.Deploy(_deployArgumentsMock.Object));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<Exception>(exception);
            Assert.Equal("TEST", exception.Message);
        }

        private static IDictionary<string, IParameter> GenerateRandomParameters()
        {
            var parameters = new Dictionary<string, IParameter>();

            var rnd = new Random();
            for (var cnt = 0; cnt < rnd.Next(30, 100); cnt++)
            {
                var parameterMock = new Mock<IParameter>();
                parameterMock.Setup(p => p.Name).Returns(Fakes.RandomString());
                parameterMock.Setup(p => p.Value).Returns(Fakes.RandomString());
                parameterMock.Setup(p => p.ParameterDataType).Returns(typeof(string));
                parameterMock.Setup(p => p.Sensitive).Returns(Fakes.RandomBool());
                parameterMock.Setup(p => p.Source).Returns(Fakes.RandomEnum<ParameterSource>());

                parameters.Add(parameterMock.Object.Name, parameterMock.Object);
            }
            return parameters;
        }
    }
}
