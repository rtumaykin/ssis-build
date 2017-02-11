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
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ConfigurationTests : IDisposable
    {
        [Fact]
        public void Pass_New()
        {
            var parameters = new Dictionary<string, string>
            {
                {"Parameter1", "Value1"},
                {"Parameter2", "Value2"}
            };

            var name = "Development";

            var xml = GetXml(name, parameters);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            var config = new Configuration(name);

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(xml);
                    writer.Flush();
                    stream.Position = 0;

                    config.Initialize(stream, null);
                }
            }

            Assert.NotNull(config.Parameters);
            Assert.True(config.Parameters.ContainsKey("Parameter1"));
            Assert.Equal("Value1", config.Parameters["Parameter1"].Value);

        }

        [Fact]
        public void Pass_New_NoParameters()
        {
            var parameters = new Dictionary<string, string> ();

            var name = "Development";

            var xml = GetXml(name, parameters);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            var config = new Configuration(name);

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(xml);
                    writer.Flush();
                    stream.Position = 0;

                    config.Initialize(stream, null);
                }
            }

            Assert.NotNull(config.Parameters);
            Assert.True(config.Parameters.Count == 0);
        }

        [Fact]
        public void Fail_New_NoConfiguration()
        {
            var parameters = new Dictionary<string, string>();

            var name = "Development";

            var xml = GetXml(Guid.NewGuid().ToString("N"), parameters);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            var config = new Configuration(name);

            Exception exception;

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(xml);
                    writer.Flush();
                    stream.Position = 0;

                    exception = Record.Exception(() => config.Initialize(stream, null));
                }
            }

            Assert.NotNull(exception);
            Assert.IsType<InvalidConfigurationNameException>(exception);
            Assert.Equal(name, (exception as InvalidConfigurationNameException)?.ConfigurationName);
        }

        public void Dispose()
        {
            
        }

        internal static string GetXml(string configurationName, IDictionary<string, string> parameters )
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
                  <Project xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                  <Configurations>
                    <Configuration>
                      <Name>{configurationName}</Name>
                      <Options>
                        <OutputPath>bin</OutputPath>
                        <ConnectionMappings />
                        <ConnectionProviderMappings />
                        <ConnectionSecurityMappings />
                        <DatabaseStorageLocations />
                        <TargetServerVersion>SQLServer2012</TargetServerVersion>
                        <ParameterConfigurationValues>
                          {string.Join("", parameters.Select(p => GetParameterXml(p.Key, p.Value)))}
                        </ParameterConfigurationValues>
                      </Options>
                    </Configuration>
                  </Configurations>
                </Project>";
        }

        internal static string GetParameterXml(string name, string value)
        {
            return $@"<ConfigurationSetting>
                        <Id>{Guid.NewGuid():D}</Id>
                        <Name>{name}</Name>
                        <Value xsi:type=""xsd:int"">{value}</Value>
                    </ConfigurationSetting>";
        }
    }
}