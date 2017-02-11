using System.Collections.Generic;
using System.IO;

namespace SsisDeploy
{
    public interface ICatalogTools
    {
        void DeployProject(string connectionString, string folderName, string projectName, bool eraseSensitiveInfo, IDictionary<string, SensitiveParameter> parametersToDeploy, MemoryStream projectStream);

    }
}