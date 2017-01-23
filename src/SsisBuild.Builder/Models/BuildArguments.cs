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

namespace SsisBuild.Models
{
    public class BuildArguments
    {
        public string ProjectPath { get; set; }
        public string OutputFolder { get; set; }
        public string ProtectionLevel { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
        public string ConfigurationName { get; set; }
        public string ReleaseNotesFilePath { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}
