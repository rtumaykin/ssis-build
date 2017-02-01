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


using System;
using System.Globalization;
using System.IO;
using System.Linq;
using SsisBuild.Core;
using SsisBuild.Core.Helpers;
using SsisBuild.Logger;

namespace SsisBuild
{
    public class Builder : IBuilder
    {
        private readonly ILogger _logger;
        private readonly IProject _project;

        public Builder() : this (new ConsoleLogger(), new Project()) { }

        internal Builder(ILogger logger, IProject project)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (project == null)
                throw new ArgumentNullException(nameof(project));

            _logger = logger;
            _project = project;
        }

        public void Execute(IBuildArguments buildArguments)
        {
            if (buildArguments == null)
                throw new ArgumentNullException(nameof(buildArguments));

            EchoBuildArguments(buildArguments);

            _logger.LogMessage("-------------------------------------------------------------------------------");
            _logger.LogMessage($"Starting build. Loading project files from {buildArguments.ProjectPath}.");

            // Load and process project
            _project.LoadFromDtproj(buildArguments.ProjectPath, buildArguments.Configuration, buildArguments.Password);

            // replace parameter values
            foreach (var buildArgumentsParameter in buildArguments.Parameters)
            {
                _project.UpdateParameter(buildArgumentsParameter.Key, buildArgumentsParameter.Value, ParameterSource.Manual);
            }

            // parse release notes if provided
            if (!string.IsNullOrWhiteSpace(buildArguments.ReleaseNotes))
            {
                ApplyReleaseNotes(buildArguments.ReleaseNotes, _project);
            }


            EchoFinalParameterValues(_project.Parameters.Values.ToArray());

            var outputFolder = string.IsNullOrWhiteSpace(buildArguments.OutputFolder)
                    ? Path.Combine(Path.GetDirectoryName(buildArguments.ProjectPath), "bin", buildArguments.Configuration)
                    : buildArguments.OutputFolder;

            var destinationPath = Path.Combine(outputFolder, Path.ChangeExtension(Path.GetFileName(buildArguments.ProjectPath), "ispac"));

            var finalProtectionLevel = string.IsNullOrWhiteSpace(buildArguments.ProtectionLevel)
                ? _project.ProtectionLevel
                : (ProtectionLevel)Enum.Parse(typeof(ProtectionLevel), buildArguments.ProtectionLevel, true);

            if (finalProtectionLevel != _project.ProtectionLevel)
                _logger.LogMessage($"Changing protection level from {_project.ProtectionLevel} to {finalProtectionLevel}.");
            else
                _logger.LogMessage($"Protection Level is unchanged: {finalProtectionLevel}.");



            if (finalProtectionLevel == ProtectionLevel.DontSaveSensitive)
            {
                _project.Save(destinationPath);
            }
            else
            {
                var encryptionPassword = string.IsNullOrWhiteSpace(buildArguments.NewPassword) ? buildArguments.Password : buildArguments.NewPassword;
                if (string.IsNullOrWhiteSpace(encryptionPassword))
                    throw new PasswordRequiredException(finalProtectionLevel.ToString());

                _project.Save(destinationPath, finalProtectionLevel, encryptionPassword);
            }

        }

        private void EchoFinalParameterValues(IParameter[] parameterValues)
        {
            _logger.LogMessage("");
            _logger.LogMessage("Parameters with values unchanged:");
            foreach (var parameter in parameterValues.Where(p => p.Source == ParameterSource.Original))
            {
                if (parameter.Sensitive && parameter.Value == null)
                    _logger.LogWarning($"   Sensitive parameter [{parameter.Name}] does not have a value. It is possible that you will need to set it on the destination SQL Server post-deploy.");
                else
                    _logger.LogMessage($"   [{parameter.Name}]; Value: {parameter.Value}");

            }

            _logger.LogMessage("");
            _logger.LogMessage("Parameters with values from configuration:");
            foreach (var parameter in parameterValues.Where(p => p.Source == ParameterSource.Configuration))
            {
                // https://connect.microsoft.com/SQLServer/feedback/details/3119460
                if (parameter.Sensitive && parameter.Value == null)
                    _logger.LogWarning($"   Failed to retrieve value for sensitive parameter [{parameter.Name}] because configuration values for sensitive parameters are stored in a dtproj.user file and always encrypted by a user key. If this is a problem, please pass parameter value through Build Arguments.");
                else
                    _logger.LogMessage($"   [{parameter.Name}]; Value: {parameter.Value}");
            }

            _logger.LogMessage("");
            _logger.LogMessage("Parameters with values from Buld Parameter Arguments:");
            foreach (var parameter in parameterValues.Where(p => p.Source == ParameterSource.Manual))
                _logger.LogMessage($"   [{parameter.Name}]; Value: {parameter.Value}");
        }

        private void ApplyReleaseNotes(string releaseNotesFilePath, IProject project)
        {
            if (File.Exists(releaseNotesFilePath))
            {
                _logger.LogMessage("");
                _logger.LogMessage("Processing Release Notes.");
                var releaseNotes = ReleaseNotesHelper.ParseReleaseNotes(releaseNotesFilePath);

                _logger.LogMessage($"   Overriding Version to {releaseNotes.Version}");


                project.VersionMajor = releaseNotes.Version.Major.ToString(CultureInfo.InvariantCulture);
                project.VersionMinor = releaseNotes.Version.Minor.ToString(CultureInfo.InvariantCulture);
                project.VersionBuild = releaseNotes.Version.Build.ToString(CultureInfo.InvariantCulture);
                _logger.LogMessage($"   Adding Release Notes {string.Join("\r\n", releaseNotes.Notes)}");
                project.VersionComments = string.Join("\r\n", releaseNotes.Notes);
                project.Description = string.Join("\r\n", releaseNotes.Notes);
            }
            else
            {
                throw new FileNotFoundException($"Release notes file does not exist.", releaseNotesFilePath);
            }
        }


        private void EchoBuildArguments(IBuildArguments buildArguments)
        {
            _logger.LogMessage("SSIS Build Engine");
            _logger.LogMessage("Copyright (c) 2017 Roman Tumaykin");
            _logger.LogMessage("");
            _logger.LogMessage("Executing SSIS Build with the following arguments:");
            _logger.LogMessage($"Project File: {buildArguments.ProjectPath}");
            if (!string.IsNullOrWhiteSpace(buildArguments.ProtectionLevel))
            {
                _logger.LogMessage($"-ProtectionLevel: {buildArguments.ProtectionLevel}");
            }

            if (!string.IsNullOrWhiteSpace(buildArguments.Password))
            {
                _logger.LogMessage("-Password: (hidden)");
            }

            if (!string.IsNullOrWhiteSpace(buildArguments.NewPassword))
            {
                _logger.LogMessage("-NewPassword: (hidden)");
            }

            if (!string.IsNullOrWhiteSpace(buildArguments.OutputFolder))
            {
                _logger.LogMessage($"-OutputFolder: {buildArguments.OutputFolder}");
            }

            if (!string.IsNullOrWhiteSpace(buildArguments.Configuration))
            {
                _logger.LogMessage($"-Configuration: {buildArguments.Configuration}");
            }
            if (!string.IsNullOrWhiteSpace(buildArguments.ReleaseNotes))
            {
                _logger.LogMessage($"-ReleaseNotes: {buildArguments.ReleaseNotes}");
            }
            _logger.LogMessage("");
            _logger.LogMessage("Project parameters:");
            foreach (var parameter in buildArguments.Parameters)
            {
                _logger.LogMessage(
                    $"  {parameter.Key}: {parameter.Value}");
            }
        }
    }
}
