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
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void Pass_New()
        {
            // Setup
            var parameters = new Dictionary<string, string>
            {
                {Fakes.RandomString(), Fakes.RandomString()},
                {Fakes.RandomString(), Fakes.RandomString()}
            };

            var name = Fakes.RandomString();

            var xml = XmlGenerators.ConfigurationFile(name, parameters);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            // Execute
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

            // Assert
            Assert.NotNull(config.Parameters);
            foreach (var parameter in parameters)
            {
                Assert.True(config.Parameters.ContainsKey(parameter.Key));
                Assert.Equal(parameter.Value, config.Parameters[parameter.Key].Value);
            }
        }

        [Fact]
        public void Pass_New_NoParameters()
        {
            // Setup
            var parameters = new Dictionary<string, string> ();

            var name = Fakes.RandomString();

            var xml = XmlGenerators.ConfigurationFile(name, parameters);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            // Execute
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

            // Assert
            Assert.NotNull(config.Parameters);
            Assert.True(config.Parameters.Count == 0);
        }

        [Fact]
        public void Fail_New_NoConfiguration()
        {
            // Setup
            var parameters = new Dictionary<string, string>();

            var name = Fakes.RandomString();

            var xml = XmlGenerators.ConfigurationFile(Fakes.RandomString(), parameters);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            // Execute
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

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidConfigurationNameException>(exception);
            Assert.Equal(name, (exception as InvalidConfigurationNameException)?.ConfigurationName);
        }
    }
}