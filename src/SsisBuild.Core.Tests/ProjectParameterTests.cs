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
using System.Xml;
using Xunit;
using SsisBuild.Core.Helpers;

namespace SsisBuild.Core.Tests
{
    public class ProjectParameterTests : IDisposable
    {
        public ProjectParameterTests()
        {
            

        }

        [Theory]
        [InlineData(false, true, DataType.String)]
        [InlineData(false, false, DataType.Int16)]
        [InlineData(true, true, DataType.String)]
        [InlineData(true, false, DataType.Int16)]
        public void Pass_New_ProjectParameter(bool sensitive, bool withValue, DataType type)
        {
            var name = Helpers.RandomString(50);
            var value = Helpers.RandomString(30);
            var scope = Helpers.RandomString(20);
            var xmldoc  = new XmlDocument();
            var parameterXml = CreateProjectParameterXml(name, sensitive, withValue, value, type);
            xmldoc.LoadXml(parameterXml);

            var parameter = new ProjectParameter(scope, xmldoc.DocumentElement);

            Assert.NotNull(parameter);
            Assert.Equal(withValue ? value : null, parameter.Value);
            Assert.Equal(type.ToString("G"), parameter.ParameterDataType.Name, StringComparer.InvariantCultureIgnoreCase);
            Assert.Equal(sensitive, parameter.Sensitive);
            Assert.Equal($"{scope}::{name}", parameter.Name);
            Assert.Equal(ParameterSource.Original, parameter.Source);
        }

        [Theory, MemberData(nameof(DataTypeValues))]
        [InlineData((DataType)1000)]
        public void Pass_New_ProjectParameter_CoverDataTypes(DataType type)
        {
            var name = Helpers.RandomString(50);
            var value = Helpers.RandomString(30);
            var scope = Helpers.RandomString(20);
            var xmldoc = new XmlDocument();
            var parameterXml = CreateProjectParameterXml(name, false, true, value, type);
            xmldoc.LoadXml(parameterXml);

            var parameter = new ProjectParameter(scope, xmldoc.DocumentElement);

            Assert.NotNull(parameter);
            Assert.Equal((int) type == 1000 ? null : type.ToString("G"), parameter.ParameterDataType?.Name, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Pass_New_ProjectParameter_InvalidDataType()
        {
            var name = Helpers.RandomString(50);
            var value = Helpers.RandomString(30);
            var scope = Helpers.RandomString(20);
            var xmldoc = new XmlDocument();
            var parameterXml = CreateProjectParameterXml(name, false, true, value, DataType.Byte);

            xmldoc.LoadXml(parameterXml);

            var dataTypeNode = xmldoc.SelectSingleNode("//*[@SSIS:Name=\"DataType\"]", xmldoc.GetNameSpaceManager());
            if (dataTypeNode != null)
                dataTypeNode.InnerText = "xyz";

            var parameter = new ProjectParameter(scope, xmldoc.DocumentElement);

            Assert.NotNull(parameter);
            Assert.Equal(null, parameter.ParameterDataType?.Name);
        }

        [Fact]
        public void Fail_New_ProjectParameter_NoScope()
        {
            var name = Helpers.RandomString(50);
            var value = Helpers.RandomString(30);
            var xmldoc = new XmlDocument();
            var parameterXml = CreateProjectParameterXml(name, false, true, value, DataType.Byte);

            xmldoc.LoadXml(parameterXml);

            var exception = Record.Exception(() => new ProjectParameter(null, xmldoc.DocumentElement));

            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void Fail_New_ProjectParameter_EmptyName()
        {
            var name = string.Empty;
            var value = Helpers.RandomString(30);
            var xmldoc = new XmlDocument();
            var parameterXml = CreateProjectParameterXml(name, false, true, value, DataType.Byte);

            xmldoc.LoadXml(parameterXml);

            var exception = Record.Exception(() => new ProjectParameter(Helpers.RandomString(50), xmldoc.DocumentElement));

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.Equal(xmldoc.DocumentElement?.OuterXml, ((InvalidXmlException)exception).NodeXml);
            Assert.NotNull(exception.Message);
        }

        [Fact]
        public void Fail_New_ProjectParameter_NoProperties()
        {
            var name = Helpers.RandomString(30);
            var value = Helpers.RandomString(30);
            var xmldoc = new XmlDocument();
            var parameterXml = $@"<SSIS:Parameter SSIS:Name=""{name}"" xmlns:SSIS=""www.microsoft.com/SqlServer/SSIS"">
            </SSIS:Parameter>";

            xmldoc.LoadXml(parameterXml);

            var exception = Record.Exception(() => new ProjectParameter(Helpers.RandomString(50), xmldoc.DocumentElement));

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
            Assert.Equal(xmldoc.DocumentElement?.OuterXml, ((InvalidXmlException) exception).NodeXml);
            Assert.NotNull(exception.Message);
        }

        [Fact]
        public void Fail_New_NoXml()
        {
            var exception = Record.Exception(() => new ProjectParameter(Helpers.RandomString(50), null));

            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [InlineData(true, true, ParameterSource.Manual)]
        [InlineData(true, false, ParameterSource.Manual)]
        [InlineData(false, true, ParameterSource.Configuration)]
        [InlineData(false, false, ParameterSource.Configuration)]
        public void Pass_SetValue(bool originalNull, bool setToNull, ParameterSource source)
        {
            var name = Helpers.RandomString(50);
            var value = Helpers.RandomString(30);
            var scope = Helpers.RandomString(20);
            var xmldoc = new XmlDocument();
            var parameterXml = CreateProjectParameterXml(name, false, originalNull, value, DataType.String);

            xmldoc.LoadXml(parameterXml);

            var parameter = new ProjectParameter(scope, xmldoc.DocumentElement);

            var newValue = setToNull ? null : Helpers.RandomString(30);

            parameter.SetValue(newValue, source);
            var testValueFromXml = xmldoc.SelectSingleNode("//*[@SSIS:Name=\"Value\"]", xmldoc.GetNameSpaceManager())?.InnerText;

            Assert.Equal(newValue, parameter.Value);
            Assert.Equal(source, parameter.Source);
            Assert.Equal(newValue, testValueFromXml);
        }

        public void Dispose()
        {
            
        }

        private static string CreateProjectParameterXml(string name, bool sensitive, bool withValue, string value, DataType dataType)
        {

            var sensitiveValue = sensitive ? "1" : "0";
            var valueElement = withValue ? $"<SSIS:Property SSIS:Name=\"Value\">{value}</SSIS:Property>" : null;


            return $@"<SSIS:Parameter SSIS:Name=""{name}"" xmlns:SSIS=""www.microsoft.com/SqlServer/SSIS"">
                <SSIS:Properties>
                <SSIS:Property SSIS:Name=""ID""></SSIS:Property>
                <SSIS:Property SSIS:Name=""CreationName""></SSIS:Property>
                <SSIS:Property SSIS:Name=""Description""></SSIS:Property>
                <SSIS:Property SSIS:Name=""IncludeInDebugDump"">0</SSIS:Property>
                <SSIS:Property SSIS:Name=""Required"">0</SSIS:Property>
                <SSIS:Property SSIS:Name=""Sensitive"">{sensitiveValue}</SSIS:Property>
                {valueElement}
                <SSIS:Property SSIS:Name=""DataType"">{dataType:D}</SSIS:Property>
                </SSIS:Properties>
            </SSIS:Parameter>";
        }

        public static IEnumerable<object[]> DataTypeValues
        {
            get
            {
                foreach (var type in Enum.GetValues(typeof(DataType)))
                {
                    yield return new[] {type};
                }
            }
        }
    }

    
}
