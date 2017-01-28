using System.Collections.Generic;

namespace SsisBuild
{
    public interface IBuildArguments
    {
        string ProjectPath { get; }
        string OutputFolder { get; }
        string ProtectionLevel { get; }
        string Password { get; }
        string NewPassword { get; }
        string Configuration { get; }
        string ReleaseNotes { get; }
        IReadOnlyDictionary<string, string> Parameters { get; }

        void ProcessArgs(string[] args);
    }
}