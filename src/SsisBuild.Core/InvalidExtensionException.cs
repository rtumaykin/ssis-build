using System;

namespace SsisBuild.Core
{
    public class InvalidExtensionException : Exception
    {
        public InvalidExtensionException(string path, string extension) : base ($"File {path} must have a .{extension} extension.") { }
    }
}
