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
using System.Linq;
using SsisBuild.Tests.Helpers;

namespace SsisBuild.Core.Tests
{
    public static class XmlGenerators
    {
        public static string ProjectConnectionsFile()
        {
            return $@"<?xml version=""1.0""?>
                <DTS:ConnectionManager xmlns:DTS=""www.microsoft.com/SqlServer/Dts""
                  DTS:ObjectName=""{Fakes.RandomString()}""
                  DTS:DTSID=""{Guid.NewGuid():B}""
                  DTS:CreationName=""{Fakes.RandomString()}"">
                  <DTS:ObjectData>
                  </DTS:ObjectData>
                </DTS:ConnectionManager>";
        }

        public static string ProjectParamsFile(IList<ParameterSetupData> parameters)
        {
            return $@"<?xml version=""1.0""?>
                <SSIS:Parameters xmlns:SSIS=""www.microsoft.com/SqlServer/SSIS"">
                {String.Join("", parameters.Select(p => ProjectFileParameter(p.Name, p.Value, p.Sensitive, p.DataType)))}
            </SSIS:Parameters>";
        }

        public static string ProjectFileParameter(string name, string value, bool sensitive, DataType dataType, bool hasValue = true)
        {
            var sensitiveInt = sensitive ? 1 : 0;
            var sensitiveAttr = sensitive ? "SSIS:Sensitive =\"1\"" : null;
            var valueElement = hasValue ? $@"<SSIS:Property SSIS:Name=""Value"" {sensitiveAttr}>{value}</SSIS:Property>" : null;

            return $@"<SSIS:Parameter SSIS:Name=""{name}"" xmlns:SSIS=""www.microsoft.com/SqlServer/SSIS"">
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
                  {valueElement}
                  <SSIS:Property
                    SSIS:Name=""DataType"">{dataType:D}</SSIS:Property>
                </SSIS:Properties>
              </SSIS:Parameter>";
        }

        public static string ConfigurationFile(string configurationName, IDictionary<string, string> parameters)
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
                          {String.Join("", parameters.Select(p => ConfigurationParameter(p.Key, p.Value)))}
                        </ParameterConfigurationValues>
                      </Options>
                    </Configuration>
                  </Configurations>
                </Project>";
        }

        internal static string UserConfigurationFile(string configurationName, IDictionary<string, string> parameters)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
                  <DataTransformationsUserConfiguration xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                  <Configurations>
                    <Configuration>
                      <Name>{configurationName}</Name>
                      <Options>
                        <ParameterConfigurationSensitiveValues>
                          {string.Join("", parameters.Select(p => ConfigurationParameter(p.Key, p.Value)))}
                        </ParameterConfigurationSensitiveValues>
                      </Options>
                    </Configuration>
                  </Configurations>
                </DataTransformationsUserConfiguration>";
        }

        private static string ConfigurationParameter(string name, string value)
        {
            return $@"<ConfigurationSetting>
                        <Id>{Guid.NewGuid():D}</Id>
                        <Name>{name}</Name>
                        <Value xsi:type=""xsd:int"">{value}</Value>
                    </ConfigurationSetting>";
        }

        public static string PackageFile(string sensitiveParameterValue, int protectionLevel, string sensitivePasswordValue)
        {
            return $@"<?xml version=""1.0""?>
                <DTS:Executable xmlns:DTS=""www.microsoft.com/SqlServer/Dts""
                DTS:ProtectionLevel=""{protectionLevel}"">
                 <DTS:ConnectionManager DTS:ConnectionString=""{Fakes.RandomString()}"">
                   <DTS:Password DTS:Name=""{Fakes.RandomString()}"" Sensitive=""1"">{sensitivePasswordValue}</DTS:Password>
                </DTS:ConnectionManager>
                <DTS:PackageParameter DTS:Sensitive=""True"">
                    <DTS:Property DTS:Name=""ParameterValue"">{sensitiveParameterValue}</DTS:Property>
                </DTS:PackageParameter>
                </DTS:Executable>";
        }


        public static string ProjectManifestFile(ProtectionLevel protectionLevel, int versionMajor, int versionMinor, string versionComments, int versionBuild, string description,
            string[] packages, string[] connectionManagers, ParameterSetupData[] parameters)
        {
            return $@"<SSIS:Project SSIS:ProtectionLevel=""{protectionLevel:G}"" xmlns:SSIS=""www.microsoft.com/SqlServer/SSIS"">
	            <SSIS:Properties>
	              <SSIS:Property SSIS:Name=""ID"">{Guid.NewGuid():B}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""Name"">{Fakes.RandomString()}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""VersionMajor"">{versionMajor}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""VersionMinor"">{versionMinor}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""VersionBuild"">{versionBuild}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""VersionComments"">{versionComments}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""CreationDate""></SSIS:Property>
	              <SSIS:Property SSIS:Name=""CreatorName"">{Fakes.RandomString()}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""CreatorComputerName"">{Fakes.RandomString()}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""Description"">{description}</SSIS:Property>
	              <SSIS:Property SSIS:Name=""FormatVersion"">1</SSIS:Property>
	            </SSIS:Properties>
	            <SSIS:Packages>
	              {String.Join("", packages.Select(ProjectManifestFile_Package))}
	            </SSIS:Packages>
	            <SSIS:ConnectionManagers>
	              {String.Join("", connectionManagers.Select(ProjectManifestFile_ConnectionManager))}
	            </SSIS:ConnectionManagers>
	            <SSIS:DeploymentInfo>
	              <SSIS:ProjectConnectionParameters>
		            {String.Join("", parameters.Select<ParameterSetupData, string>(p => ProjectFileParameter(p.Name, p.Value, p.Sensitive, p.DataType)))}
	              </SSIS:ProjectConnectionParameters>
                  <SSIS:PackageInfo>
                    {String.Join("",
                packages.Select<string, string>(p => ProjectManifestFile_PackageMetadata(p, parameters, versionMajor, versionMinor, versionBuild, versionComments, protectionLevel)))}
                  </SSIS:PackageInfo>
                </SSIS:DeploymentInfo>		  
            </SSIS:Project>";
        }

        private static string ProjectManifestFile_PackageMetadata(string packageName, ParameterSetupData[] parameters, int versionMajor, int versionMinor, int versionBuild,
            string versionComments, ProtectionLevel protectionLevel)
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
    		            {String.Join("", parameters.Select<ParameterSetupData, string>(p => ProjectFileParameter(p.Name, p.Value, p.Sensitive, p.DataType)))}
                      </SSIS:Parameters>
	                </SSIS:PackageMetaData>";
        }

        private static string ProjectManifestFile_Package(string name)
        {
            return $@"<SSIS:Package SSIS:Name=""{name}"" SSIS:EntryPoint=""1"" />";
        }

        private static string ProjectManifestFile_ConnectionManager(string name)
        {
            return $@"<SSIS:ConnectionManager SSIS:Name=""{name}"" />";
        }
    }
}
