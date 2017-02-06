using System.Collections.Generic;
using System.IO;

namespace SsisBuild.Core
{
    public interface IProjectFile
    {
        IReadOnlyDictionary<string, IParameter> Parameters { get; }
        ProtectionLevel ProtectionLevel { get; set; }

        void Initialize(string filePath, string password);
        void Initialize(Stream fileStream, string password);
        void Save(string filePath);
        void Save(Stream fileStream);
        void Save(string filePath, ProtectionLevel protectionLevel, string password);
        void Save(Stream fileStream, ProtectionLevel protectionLevel, string password);
    }
}