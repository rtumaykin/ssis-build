using System;
using System.Xml;
using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ConfigurationParameterTests : IDisposable
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Pass_CreateNew(bool sensitive)
        {
            var name = Helpers.RandomString(30);
            var value = Helpers.RandomString(20);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(CreateXml(name, value));

            var param = new ConfigurationParameter(xmlDoc.DocumentElement, sensitive);

            Assert.NotNull(param);
            Assert.Equal(name, param.Name);
            Assert.Equal(sensitive ? null : value, param.Value);
            Assert.Equal(sensitive, param.Sensitive);
        }

        [Fact]
        public void Fail_InvalidXmlIdMissing()
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<root>ldkldkd</root>");

            var exception = Record.Exception(() => new ConfigurationParameter(xmlDoc.DocumentElement, false));

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
        }

        [Fact]
        public void Fail_InvalidXmlNameissing()
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<root><Id>{DE2BEF67-8332-43A6-B634-49E46FC66E20}</Id><aaa>ldkldkd</aaa></root>");

            var exception = Record.Exception(() => new ConfigurationParameter(xmlDoc.DocumentElement, false));

            Assert.NotNull(exception);
            Assert.IsType<InvalidXmlException>(exception);
        }

        private string CreateXml(string name, string value)
        {
            var rootnode = $"abc{Helpers.RandomString(30)}";
            return $@"<{rootnode} xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
            <Id>{Guid.NewGuid():B}</Id>
            <Name>{name}</Name>
            <Value xsi:type=""xsd:int"">{value}</Value>
          </{rootnode}>";
        } 


        public void Dispose()
        {
        }
    }
}