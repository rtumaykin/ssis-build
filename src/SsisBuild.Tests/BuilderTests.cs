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
using System.IO;
using System.Linq;
using System.Xml;
using Moq;
using SsisBuild.Core;
using SsisBuild.Logger;
using Xunit;

namespace SsisBuild.Tests
{
    public class BuilderTests : IDisposable
    {
        private class TestLogger : ILogger
        {
            public IList<string> Messages { get; private set; }
            public IList<string> Warnings{ get; private set; }
            public IList<string> Errors { get; private set; }

            public TestLogger()
            {
                Messages = new List<string>();
                Warnings = new List<string>();
                Errors = new List<string>();
            }
            public void LogMessage(string message)
            {
                Messages.Add(message);
            }

            public void LogError(string error)
            {
                Errors.Add(error);
            }

            public void LogWarning(string warning)
            {
                Warnings.Add(warning);
            }
        }

        private class TestBuildParameterUpdateResult
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public ParameterSource Source { get; set; }
        }

        private class TestBuildParameterUpdateInput
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private readonly Mock<IProject> _projectMock;
        private readonly Mock<IBuildArguments> _buildArgumentsMock;
        private string _workingFolder;

        public BuilderTests()
        {
            _projectMock = new Mock<IProject>();
            _buildArgumentsMock = new Mock<IBuildArguments>();
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingFolder);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, "EncryptAllWithPassword", true, false)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, "EncryptAllWithPassword", true, true)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, "EncryptSensitiveWithPassword", true, false)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, "EncryptSensitiveWithPassword", true, true)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, "DontSaveSensitive", true, false)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, null, true, false)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, null, true, true)]

        [InlineData(ProtectionLevel.EncryptAllWithPassword, "EncryptAllWithPassword", true, false)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword, "EncryptAllWithPassword", true, true)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword, "EncryptSensitiveWithPassword", true, false)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword, "EncryptSensitiveWithPassword", true, true)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword, "DontSaveSensitive", true, false)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword, null, true, true)]

        [InlineData(ProtectionLevel.DontSaveSensitive, "EncryptAllWithPassword", true, false)]
        [InlineData(ProtectionLevel.DontSaveSensitive, "EncryptAllWithPassword", true, true)]
        [InlineData(ProtectionLevel.DontSaveSensitive, "EncryptAllWithPassword", false, true)]
        [InlineData(ProtectionLevel.DontSaveSensitive, "EncryptSensitiveWithPassword", true, false)]
        [InlineData(ProtectionLevel.DontSaveSensitive, "EncryptSensitiveWithPassword", true, true)]
        [InlineData(ProtectionLevel.DontSaveSensitive, "EncryptSensitiveWithPassword", false, true)]
        [InlineData(ProtectionLevel.DontSaveSensitive, "DontSaveSensitive", false, false)]
        [InlineData(ProtectionLevel.DontSaveSensitive, "DontSaveSensitive", true, false)]
        [InlineData(ProtectionLevel.DontSaveSensitive, null, true, true)]
        [InlineData(ProtectionLevel.DontSaveSensitive, null, false, false)]
        public void Pass_Execute_EncryptionLevels_Passwords(ProtectionLevel projectProtectionLevel, string buildArgumentsProtectionLevel, bool sendPassword, bool sendNewPassword)
        {
            var password = sendPassword ? Helpers.RandomString(30) : null;
            var newPassword = sendNewPassword ? Helpers.RandomString(30) : null;

            _projectMock.Setup(p => p.ProtectionLevel).Returns(projectProtectionLevel);
            _projectMock.Setup(p => p.Parameters).Returns(new Dictionary<string, IParameter>());

            _buildArgumentsMock.Setup(ba => ba.ProtectionLevel).Returns(buildArgumentsProtectionLevel);
            _buildArgumentsMock.Setup(ba => ba.ProjectPath).Returns(Path.GetTempFileName());
            _buildArgumentsMock.Setup(ba => ba.Password).Returns(password);
            _buildArgumentsMock.Setup(ba => ba.NewPassword).Returns(newPassword);
            _buildArgumentsMock.Setup(ba => ba.Parameters).Returns(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
            _buildArgumentsMock.Setup(ba => ba.Configuration).Returns(Helpers.RandomString(30));

            var project = _projectMock.Object;
            var buildArguments = _buildArgumentsMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(buildArguments));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, "EncryptAllWithPassword", false, false)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword, "EncryptSensitiveWithPassword", false, false)]

        [InlineData(ProtectionLevel.EncryptAllWithPassword, "EncryptAllWithPassword", false, false)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword, "EncryptSensitiveWithPassword", false, false)]

        [InlineData(ProtectionLevel.DontSaveSensitive, "EncryptAllWithPassword", false, false)]
        [InlineData(ProtectionLevel.DontSaveSensitive, "EncryptSensitiveWithPassword", false, false)]
        public void Fail_Execute_EncryptionLevels_Passwords(ProtectionLevel projectProtectionLevel, string buildArgumentsProtectionLevel, bool sendPassword, bool sendNewPassword)
        {
            var password = sendPassword ? Helpers.RandomString(30) : null;
            var newPassword = sendNewPassword ? Helpers.RandomString(30) : null;

            _projectMock.Setup(p => p.ProtectionLevel).Returns(projectProtectionLevel);
            _projectMock.Setup(p => p.Parameters).Returns(new Dictionary<string, IParameter>());

            _buildArgumentsMock.Setup(ba => ba.ProtectionLevel).Returns(buildArgumentsProtectionLevel);
            _buildArgumentsMock.Setup(ba => ba.ProjectPath).Returns(Path.GetTempFileName());
            _buildArgumentsMock.Setup(ba => ba.Password).Returns(password);
            _buildArgumentsMock.Setup(ba => ba.NewPassword).Returns(newPassword);
            _buildArgumentsMock.Setup(ba => ba.Parameters).Returns(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
            _buildArgumentsMock.Setup(ba => ba.Configuration).Returns(Helpers.RandomString(30));

            var project = _projectMock.Object;
            var buildArguments = _buildArgumentsMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(buildArguments));
            Assert.NotNull(exception);
            Assert.IsType<PasswordRequiredException>(exception);
        }

        [Fact]
        public void Pass_Execute_BuildArgumentsParametersUpdate()
        {
            var outputParameters = new List<TestBuildParameterUpdateResult>();
            var inputParameers = new List<TestBuildParameterUpdateInput>();

            var paramsCount = new Random(DateTime.Now.Millisecond).Next(20, 100);
            for (var i = 0; i < paramsCount; i++)
            {
                inputParameers.Add(new TestBuildParameterUpdateInput()
                {
                    Value = Helpers.RandomString(paramsCount),
                    // to ensure it is unique
                    Name = $"{Guid.NewGuid():N}-{Helpers.RandomString(paramsCount)}"
                });
            }

            _projectMock.Setup(p => p.ProtectionLevel).Returns(ProtectionLevel.DontSaveSensitive);
            _projectMock.Setup(p => p.Parameters).Returns(new Dictionary<string, IParameter>());
            _projectMock.Setup(p => p.UpdateParameter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ParameterSource>())).Callback(
                (string parameterName, string value, ParameterSource parameterSource) =>
                {
                    outputParameters.Add(new TestBuildParameterUpdateResult
                    {
                        Name = parameterName,
                        Value = value,
                        Source = parameterSource
                    });
                });

            _buildArgumentsMock.Setup(ba => ba.ProjectPath).Returns(Path.GetTempFileName());
            _buildArgumentsMock.Setup(ba => ba.Parameters).Returns(new ReadOnlyDictionary<string, string>(inputParameers.ToDictionary(i => i.Name, i => i.Value)));
            _buildArgumentsMock.Setup(ba => ba.Configuration).Returns(Helpers.RandomString(30));

            var project = _projectMock.Object;
            var buildArguments = _buildArgumentsMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(buildArguments));

            Assert.Null(exception);
            Assert.True(outputParameters.Count == paramsCount);
            for (var cnt = 0; cnt < paramsCount; cnt++)
            {
                Assert.True(
                    outputParameters[cnt].Value == inputParameers[cnt].Value
                    && outputParameters[cnt].Name == inputParameers[cnt].Name
                    && outputParameters[cnt].Source == ParameterSource.Manual
                );
            }
        }

        [Theory]
        [InlineData("*1.0.0 - Initial release")]
        [InlineData("### New in 1.1.10 (Released 2017/01/01)\r\n* Note 1.\r\n* Note 2.\r\n* Note3.\r\n")]
        [InlineData("*1.0.0 - Initial release\r\n\r\n*0.9.0 - Pre-release")]
        [InlineData("### New in 1.1.10 (Released 2017/01/01)\r\n* Note 1.\r\n* Note 2.\r\n* Note3.\r\n\r\n### New in 1.1.09 (Released 2016/12/01)\r\n* Note 4.\r\n* Note 5.\r\n* Note6.\r\n")]
        public void Execute_Pass_ValidReleaseNotes(string releaseNotesWithLineBreaks)
        {
            var releaseNotesPath = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(releaseNotesPath, releaseNotesWithLineBreaks);

            _projectMock.Setup(p => p.ProtectionLevel).Returns(ProtectionLevel.DontSaveSensitive);
            _projectMock.Setup(p => p.Parameters).Returns(new Dictionary<string, IParameter>());

            _buildArgumentsMock.Setup(ba => ba.ProjectPath).Returns(Path.GetTempFileName());
            _buildArgumentsMock.Setup(ba => ba.Parameters).Returns(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
            _buildArgumentsMock.Setup(ba => ba.Configuration).Returns(Helpers.RandomString(30));
            _buildArgumentsMock.Setup(ba => ba.ReleaseNotes).Returns(releaseNotesPath);

            var project = _projectMock.Object;
            var buildArguments = _buildArgumentsMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(buildArguments));
            Assert.Null(exception);
        }

        [Fact]
        public void Execute_Fail_InvalidPathReleaseNotes()
        {
            var releaseNotesPath = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));

            _projectMock.Setup(p => p.ProtectionLevel).Returns(ProtectionLevel.DontSaveSensitive);
            _projectMock.Setup(p => p.Parameters).Returns(new Dictionary<string, IParameter>());

            _buildArgumentsMock.Setup(ba => ba.ProjectPath).Returns(Path.GetTempFileName());
            _buildArgumentsMock.Setup(ba => ba.Parameters).Returns(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
            _buildArgumentsMock.Setup(ba => ba.Configuration).Returns(Helpers.RandomString(30));
            _buildArgumentsMock.Setup(ba => ba.ReleaseNotes).Returns(releaseNotesPath);

            var project = _projectMock.Object;
            var buildArguments = _buildArgumentsMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(buildArguments));
            Assert.NotNull(exception);
            Assert.IsType<FileNotFoundException>(exception);
            Assert.Equal(((FileNotFoundException) exception).FileName, releaseNotesPath);
        }

        [Theory]
        [InlineData("#*1xd.0.0&&Initial release")]
        [InlineData("lalala*1xd.0.0&&Initial release")]
        [InlineData("*1q.0ew.0 - Initial release")]
        [InlineData(null)]
        public void Execute_Fail_InvalidContentReleaseNotes(string releaseNotes)
        {
            var releaseNotesPath = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.Create(releaseNotesPath).Close();
            if (releaseNotes != null)
                File.WriteAllText(releaseNotesPath, releaseNotes);

            _projectMock.Setup(p => p.ProtectionLevel).Returns(ProtectionLevel.DontSaveSensitive);
            _projectMock.Setup(p => p.Parameters).Returns(new Dictionary<string, IParameter>());

            _buildArgumentsMock.Setup(ba => ba.ProjectPath).Returns(Path.GetTempFileName());
            _buildArgumentsMock.Setup(ba => ba.Parameters).Returns(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
            _buildArgumentsMock.Setup(ba => ba.Configuration).Returns(Helpers.RandomString(30));
            _buildArgumentsMock.Setup(ba => ba.ReleaseNotes).Returns(releaseNotesPath);

            var project = _projectMock.Object;
            var buildArguments = _buildArgumentsMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(buildArguments));
            Assert.NotNull(exception);
            Assert.IsType<InvalidReleaseNotesException>(exception);
        }

        [Fact]
        public void Execute_Pass_CustomOutputFolder()
        {
            _projectMock.Setup(p => p.ProtectionLevel).Returns(ProtectionLevel.DontSaveSensitive);
            _projectMock.Setup(p => p.Parameters).Returns(new Dictionary<string, IParameter>());

            _buildArgumentsMock.Setup(ba => ba.ProjectPath).Returns(Path.GetTempFileName());
            _buildArgumentsMock.Setup(ba => ba.Parameters).Returns(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
            _buildArgumentsMock.Setup(ba => ba.Configuration).Returns(Helpers.RandomString(30));
            _buildArgumentsMock.Setup(ba => ba.OutputFolder).Returns("something");

            var project = _projectMock.Object;
            var buildArguments = _buildArgumentsMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(buildArguments));
            Assert.Null(exception);
        }


        [Fact]
        public void Execute_Pass_EchoParametersCoverage()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><parent><child>123</child></parent></root>");
            var childNode = xmlDocument.SelectSingleNode("/root/parent/child") as XmlElement;


            var projectParameters = new Dictionary<string, IParameter>
            {
                {"Parameter1", CreateParameter("Parameter1", "1", false, ParameterSource.Original)},
                {"SensitiveOriginalParameterNoValue", CreateParameter("SensitiveOriginalParameterNoValue", null, true, ParameterSource.Original)},
                {"Parameter2", CreateParameter("Parameter2", "1", false, ParameterSource.Configuration)},
                {"SensitiveConfigParameterNoValue", CreateParameter("SensitiveConfigParameterNoValue", null, true, ParameterSource.Configuration)},
                {"Parameter3", CreateParameter("Parameter3", "1", false, ParameterSource.Manual)}
            };

            _projectMock.Setup(p => p.ProtectionLevel).Returns(ProtectionLevel.DontSaveSensitive);
            _projectMock.Setup(p => p.Parameters).Returns(projectParameters);

            _buildArgumentsMock.Setup(ba => ba.ProjectPath).Returns(Path.GetTempFileName());
            _buildArgumentsMock.Setup(ba => ba.Parameters).Returns(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
            _buildArgumentsMock.Setup(ba => ba.Configuration).Returns(Helpers.RandomString(30));

            var project = _projectMock.Object;
            var buildArguments = _buildArgumentsMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(buildArguments));
            Assert.Null(exception);
        }


        [Fact]
        public void Fail_Execute_NoArgs()
        {
            var project = _projectMock.Object;
            var logger = new TestLogger();
            var builder = new Builder(logger, project);
            var exception = Record.Exception(() => builder.Execute(null));
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public void Fail_New_NullLoggerOrProject(bool passLogger, bool passProject)
        {
            var logger = passLogger ? new TestLogger() : null;
            var project = passProject ? _projectMock.Object : null;

            var exception = Record.Exception(() => new Builder(logger, project));
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void Pass_New()
        {
            var builder = new Builder();
            Assert.NotNull(builder);
        }

        private IParameter CreateParameter(string name, string value, bool sensitive, ParameterSource source)
        {
            var parameterMock = new Mock<IParameter>();
            parameterMock.Setup(p => p.Name).Returns(name);
            parameterMock.Setup(p => p.Sensitive).Returns(sensitive);
            parameterMock.Setup(p => p.Value).Returns(value);
            parameterMock.Setup(p => p.Source).Returns(source);

            return parameterMock.Object;
        }

        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
        }
    }
}
