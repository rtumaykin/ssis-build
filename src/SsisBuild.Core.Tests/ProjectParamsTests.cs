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
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ProjectParamsTests : IDisposable
    {
        private readonly string _workingFolder;

        public ProjectParamsTests()
        {
            _workingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingFolder);
        }

        [Theory, MemberData(nameof(ParameterData))]
        public void Pass_New(IList<ParameterSetupData> parameters)
        {
            // Setup
            var xml = XmlGenerators.ProjectParamsFile(parameters);
            var path = Path.Combine(_workingFolder, Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, xml);

            // Execute
            var projectParams = new ProjectParams();
            projectParams.Initialize(path, null);

            // Assert
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
        
        public void Dispose()
        {
            Directory.Delete(_workingFolder, true);
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
                        Name = Fakes.RandomString(),
                        Value = Fakes.RandomString(),
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