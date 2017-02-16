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

using Xunit;

namespace SsisBuild.Core.Tests
{
    public class ProjectConnectionTests 
    {
        [Theory]
        [InlineData(ProtectionLevel.EncryptSensitiveWithPassword)]
        [InlineData(ProtectionLevel.EncryptAllWithUserKey)]
        [InlineData(ProtectionLevel.EncryptSensitiveWithUserKey)]
        [InlineData(ProtectionLevel.EncryptAllWithPassword)]
        [InlineData(ProtectionLevel.DontSaveSensitive)]
        [InlineData(ProtectionLevel.ServerStorage)]
        public void Pass_ProtectionLevel_ProjectFile(ProtectionLevel protectionLevel)
        {
            // Execute
            var projectConnection = new ProjectConnection {ProtectionLevel = protectionLevel};

            // Assert
            Assert.Equal(protectionLevel, projectConnection.ProtectionLevel);
        }
        
    }
}