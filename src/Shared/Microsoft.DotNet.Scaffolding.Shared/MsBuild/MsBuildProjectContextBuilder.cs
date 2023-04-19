// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json;

namespace Microsoft.Extensions.ProjectModel
{
    public class MsBuildProjectContextBuilder
    {
        private string _projectPath;
        private string _targetLocation;
        private string _configuration;

        public MsBuildProjectContextBuilder(string projectPath, string targetsLocation, string configuration = "Debug")
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentException(MessageStrings.ProjectPathNotGiven);
            }

            if (string.IsNullOrEmpty(targetsLocation))
            {
                throw new ArgumentException(MessageStrings.TargetLocationNotGiven);
            }

            _configuration = configuration;
            _projectPath = projectPath;
            _targetLocation = targetsLocation;
        }

        public IProjectContext Build()
        {
            var errors = new List<string>();
            var output = new List<string>();
            var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var result = Command.CreateDotNet(
                "msbuild",
                new string[]
                {
                    _projectPath,
                    $"/t:EvaluateProjectInfoForCodeGeneration",
                    $"/p:OutputFile={tmpFile};CodeGenerationTargetLocation={_targetLocation};Configuration={_configuration}",
                    "-restore"
                })
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();

            if (result.ExitCode != 0)
            {
                throw CreateProjectContextCreationFailedException(_projectPath, errors);
            }
            try
            {
                var info = File.ReadAllText(tmpFile);

                var buildContext = JsonConvert.DeserializeObject<CommonProjectContext>(info);

                return buildContext;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read the BuildContext information.", ex);
            }
        }

        private Exception CreateProjectContextCreationFailedException(string _projectPath, List<string> errors)
        {
            var errorMsg = $"Failed to get Project Context for {_projectPath}.";
            if (errors != null)
            {
                errorMsg += $"{Environment.NewLine} {string.Join(Environment.NewLine, errors)} ";
            }

            return new InvalidOperationException(errorMsg);
        }
    }
}
