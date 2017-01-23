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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using SsisBuild.Helpers;
using SsisBuild.Logger;
using SsisBuild.Models;

namespace SsisBuild
{
    public class Builder
    {
        private readonly ILogger _logger;

        public Builder(ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            _logger = logger;
        }

        public void Execute(BuildArguments buildArguments)
        {
            EchoBuildArguments(buildArguments);

            // steps
            // 1. Load project ==>
            // 1.1. Extract manifest
            // 1.2. Load selected configuration
            // 1.3. Make sure that deployment mode is project
            // ==> don't need dtproj any more.
            _logger.LogMessage("-------------------------------------------------------------------------------");
            _logger.LogMessage($"Starting build. Loading project files from {buildArguments.ProjectPath}.");


            var project = Project.LoadFromDtproj(buildArguments.ProjectPath, buildArguments.ConfigurationName, buildArguments.Password);

            foreach (var buildArgumentsParameter in buildArguments.Parameters)
            {
                project.UpdateParameter(buildArgumentsParameter.Key, buildArgumentsParameter.Value, ParameterSource.Manual);
            }

            _logger.LogMessage("");
            _logger.LogMessage("Parameters with values unchanged:");
            foreach (var parameter in project.Parameters.Values.Where(p => p.Source == ParameterSource.Original))
                _logger.LogMessage($"   [{parameter.Name}]; Value: {parameter.Value}");

            _logger.LogMessage("");
            _logger.LogMessage("Parameters with values from configuration:");
            foreach (var parameter in project.Parameters.Values.Where(p => p.Source == ParameterSource.Configuration))
                _logger.LogMessage($"   [{parameter.Name}]; Value: {parameter.Value??"(failed to decrypt from dtproj.user. Set to null.)"}");

            _logger.LogMessage("");
            _logger.LogMessage("Parameters with values from Buld Parameter Arguments:");
            foreach (var parameter in project.Parameters.Values.Where(p => p.Source == ParameterSource.Manual))
                _logger.LogMessage($"   [{parameter.Name}]; Value: {parameter.Value}");

            var outputFolder = string.IsNullOrWhiteSpace(buildArguments.OutputFolder)
                    ? Path.Combine(Path.GetDirectoryName(buildArguments.ProjectPath), "bin", buildArguments.ConfigurationName)
                    : buildArguments.OutputFolder;

            var destinationPath = Path.Combine(outputFolder, Path.ChangeExtension(Path.GetFileName(buildArguments.ProjectPath), "ispac"));

            project.Save(destinationPath, project.ProtectionLevel, "ssis1234");


            // 2. Process manifest ==>
            // 2.1. Get project connections
            // 2.2. Get project packages
            // 2.3. Get project parameters
            // 2.4. Decrypt all encrypted nodes
            // 2.4. Build a list of all settable parameters
            // 2.5. For each settable parameter assign value ==> either Build Parameter Argument, or configuration value, or original value
        }


        private void EchoBuildArguments(BuildArguments buildArguments)
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
                _logger.LogMessage($"-Password: (hidden)");
            }

            if (!string.IsNullOrWhiteSpace(buildArguments.NewPassword))
            {
                _logger.LogMessage($"-NewPassword: (hidden)");
            }

            if (!string.IsNullOrWhiteSpace(buildArguments.OutputFolder))
            {
                _logger.LogMessage($"-OutputFolder: {buildArguments.OutputFolder}");
            }

            if (!string.IsNullOrWhiteSpace(buildArguments.ConfigurationName))
            {
                _logger.LogMessage($"-Configuration: {buildArguments.ConfigurationName}");
            }
            if (!string.IsNullOrWhiteSpace(buildArguments.ReleaseNotesFilePath))
            {
                _logger.LogMessage($"-ReleaseNotes: {buildArguments.ReleaseNotesFilePath}");
            }
            _logger.LogMessage("");
            _logger.LogMessage("Project parameters:");
            foreach (var parameter in buildArguments.Parameters)
            {
                _logger.LogMessage(
                    $"  {parameter.Key} (Sensitive = false): {parameter.Value}");
            }
        }
    }
}
