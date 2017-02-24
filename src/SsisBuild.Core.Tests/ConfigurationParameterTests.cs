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
using System.Xml;
using SsisBuild.Core.ProjectManagement;
using SsisBuild.Tests.Helpers;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ConfigurationParameterTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Pass_CreateNew(bool sensitive)
        {
            // Setup
            var name = Fakes.RandomString();
            var value = Fakes.RandomString();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(CreateXml(name, value));

            // Execute
            var param = new ConfigurationParameter(xmlDoc.DocumentElement, sensitive);

            // Assert
            Assert.NotNull(param);
            Assert.Equal(name, param.Name);
            Assert.Equal(sensitive ? null : value, param.Value);
            Assert.Equal(sensitive, param.Sensitive);
        }

        [Fact]
        public void Fail_InvalidXmlIdMissing()
        {
            // Setup
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<root>ldkldkd</root>");

            // Execute
            var exception = Record.Exception(() => new ConfigurationParameter(xmlDoc.DocumentElement, false));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
        }

        [Fact]
        public void Fail_InvalidXmlNameissing()
        {
            // Setup
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<root><Id>{DE2BEF67-8332-43A6-B634-49E46FC66E20}</Id><aaa>ldkldkd</aaa></root>");

            // Execute
            var exception = Record.Exception(() => new ConfigurationParameter(xmlDoc.DocumentElement, false));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
        }

        private string CreateXml(string name, string value)
        {
            var rootnode = $"abc{Fakes.RandomString()}";
            return $@"<{rootnode} xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
            <Id>{Guid.NewGuid():B}</Id>
            <Name>{name}</Name>
            <Value xsi:type=""xsd:int"">{value}</Value>
          </{rootnode}>";
        } 
    }
}