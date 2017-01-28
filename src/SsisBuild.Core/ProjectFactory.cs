using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using SsisBuild.Core.Helpers;

namespace SsisBuild.Core
{
    public class ProjectFactory : IProjectFactory
    {
        public Project LoadFromIspac(string filePath, string password)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} does not exist or you don't have permissions to access it.", filePath);

            if (!filePath.EndsWith(".ispac", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"File {filePath} does not have an .ispac extension.");

            var project = new Project();

            using (var ispacStream = new FileStream(filePath, FileMode.Open))
            {
                using (var ispacArchive = new ZipArchive(ispacStream, ZipArchiveMode.Read))
                {
                    foreach (var ispacArchiveEntry in ispacArchive.Entries)
                    {
                        var fileName = ispacArchiveEntry.FullName;
                        using (var fileStream = ispacArchiveEntry.Open())
                        {
                            switch (Path.GetExtension(fileName))
                            {
                                case ".manifest":
                                    project.ManifestProjectFile = new ProjectManifest().Initialize(fileStream, password);
                                    break;

                                case ".params":
                                    project.ParametersProjectFile = new ProjectParams().Initialize(fileStream, password);
                                    break;

                                case ".dtsx":
                                    project.PackagesProjectFiles.Add(fileName, new Package().Initialize(fileStream, password));
                                    break;

                                case ".conmgr":
                                    project.ConnectionsProjectFiles.Add(fileName, new ProjectConnection().Initialize(fileStream, password));
                                    break;

                                case ".xml":
                                    break;

                                default:
                                    throw new Exception($"Unexpected file {fileName} in {filePath}.");
                            }
                        }
                    }
                }
            }

            project.LoadParameters();

            return project;
        }

        public Project LoadFromDtproj(string filePath, string configurationName, string password)
        {
            if (!File.Exists(filePath))
                throw new Exception($"File {filePath} does not exist.");

            if (!filePath.EndsWith(".dtproj", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"File {filePath} does not have an .dtproj extension.");

            var dtprojXmlDoc = new XmlDocument();
            dtprojXmlDoc.Load(filePath);
            ValidateDeploymentMode(dtprojXmlDoc);

            var nsManager = dtprojXmlDoc.GetNameSpaceManager();

            var projectXmlNode = dtprojXmlDoc.SelectSingleNode("/Project/DeploymentModelSpecificContent/Manifest/SSIS:Project", nsManager);

            if (projectXmlNode == null)
                throw new Exception("Project Manifest Node was not found.");

            var projectDirectory = Path.GetDirectoryName(filePath);

            if (string.IsNullOrWhiteSpace(projectDirectory))
                throw new Exception("Failed to retrieve directory of the source project.");

            var project = new Project();

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(projectXmlNode.OuterXml);
                    writer.Flush();
                    stream.Position = 0;

                    project.ManifestProjectFile = new ProjectManifest().Initialize(stream, password);
                }
            }

            project.ParametersProjectFile = new ProjectParams().Initialize(Path.Combine(projectDirectory, "Project.params"), password);

            foreach (var connectionManagerName in project.ProjectManifest.ConnectionManagerNames)
            {
                project.ConnectionsProjectFiles.Add(connectionManagerName, new ProjectConnection().Initialize(Path.Combine(projectDirectory, connectionManagerName), password));
            }

            foreach (var packageName in project.ProjectManifest.PackageNames)
            {
                project.PackagesProjectFiles.Add(packageName, new Package().Initialize(Path.Combine(projectDirectory, packageName), password));
            }

            project.LoadParameters();

            foreach (var configurationParameter in new Configuration(configurationName).Initialize(filePath, password).Parameters)
            {
                project.UpdateParameter(configurationParameter.Key, configurationParameter.Value.Value, ParameterSource.Configuration);
            }

            var userConfigurationFilePath = $"{filePath}.user";
            if (File.Exists(userConfigurationFilePath))
            {
                foreach (var userConfigurationParameter in new UserConfiguration(configurationName).Initialize(userConfigurationFilePath, password).Parameters)
                {
                    project.UpdateParameter(userConfigurationParameter.Key, null, ParameterSource.Configuration);
                }
            }


            return project;
        }

        private static void ValidateDeploymentMode(XmlNode dtprojXmlDoc)
        {
            var deploymentModel = dtprojXmlDoc.SelectSingleNode("/Project/DeploymentModel")?.InnerText;

            if (deploymentModel != "Project")
                throw new Exception("This build method only apply to Project deployment model.");
        }
    }
}
