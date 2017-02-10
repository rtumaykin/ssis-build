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
            var projectConnection = new ProjectConnection();
            projectConnection.ProtectionLevel = protectionLevel;
            Assert.Equal(protectionLevel, projectConnection.ProtectionLevel);
        }
        
    }
}