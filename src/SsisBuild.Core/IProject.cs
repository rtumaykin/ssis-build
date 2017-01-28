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

using System.Collections.Generic;
using System.IO;

namespace SsisBuild.Core
{
    public interface IProject
    {
        ProtectionLevel ProtectionLevel { get; }
        string VersionMajor { get; set; }
        string VersionMinor { get; set; }
        string VersionBuild { get; set; }
        string VersionComments { get; set; }
        string Description { get; set; }
        IReadOnlyDictionary<string, Parameter> Parameters { get; }


        void LoadFromIspac(string filePath, string password);
        void LoadFromDtproj(string filePath, string configurationName, string password);
        void Save(Stream destinationStream, ProtectionLevel protectionLevel, string password);
        void Save(string destinationFilePath);
        void Save(string destinationFilePath, ProtectionLevel protectionLevel, string password);
        void UpdateParameter(string parameterName, string value, ParameterSource source);
    }
}