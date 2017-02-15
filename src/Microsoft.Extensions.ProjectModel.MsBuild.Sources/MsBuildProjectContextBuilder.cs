// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Internal;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Newtonsoft.Json;

namespace Microsoft.Extensions.ProjectModel
{
    public class MsBuildProjectContextBuilder
    {
        private string _projectPath;
        private string _targetLocation;
        private string _configuration;

        public MsBuildProjectContextBuilder(string projectPath, string targetsLocation, string configuration="Debug")
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (string.IsNullOrEmpty(targetsLocation))
            {
                throw new ArgumentNullException(nameof(targetsLocation));
            }

            _configuration = configuration;
            _projectPath = projectPath;
            _targetLocation = targetsLocation;
        }

        public IProjectContext Build()
        {
            var errors = new List<string>();
            var output = new List<string>();
            var tmpFile = Path.GetTempFileName();
            var result = Command.CreateDotNet(
                "msbuild",
                new string[]
                {
                    _projectPath,
                    $"/t:EvaluateProjectInfoForCodeGeneration",
                    $"/p:OutputFile={tmpFile};CodeGenerationTargetLocation={_targetLocation};Configuration={_configuration}"
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
                errorMsg += $"{Environment.NewLine} { string.Join(Environment.NewLine, errors)} ";
            }

            return new InvalidOperationException(errorMsg);
        }
    }
}
