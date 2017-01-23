namespace SsisBuild.Models
{
    public enum ProtectionLevel
    {
        DontSaveSensitive,
        EncryptSensitiveWithUserKey,
        EncryptSensitiveWithPassword,
        EncryptAllWithPassword,
        EncryptAllWithUserKey,
        ServerStorage
    }
}
