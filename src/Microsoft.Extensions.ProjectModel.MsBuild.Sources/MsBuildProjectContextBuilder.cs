// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Newtonsoft.Json;
using NuGet.Frameworks;

namespace Microsoft.Extensions.ProjectModel
{
    public class MsBuildProjectContextBuilder
    {
        private string _projectPath;
        private NuGetFramework _targetFramework;

        public MsBuildProjectContextBuilder(string projectPath, NuGetFramework targetFramework)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            _projectPath = projectPath;
            _targetFramework = targetFramework ?? FrameworkConstants.CommonFrameworks.NetCoreApp10;
        }

        public IProjectContext Build()
        {
            var tmpFile = Path.GetTempFileName();
            var result = Command.Create("dotnet",
                new string[] 
                {
                    "msbuild",
                    _projectPath,
                    $"/t:EvaluateProjectInfoForCodeGeneration", 
                    $"/p:TargetFramework={_targetFramework.GetShortFolderName()};OutputFile={tmpFile}"
                })
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException("Build Failed.");
            }

            var info = File.ReadAllText(tmpFile);

            var buildContext = JsonConvert.DeserializeObject<MsBuildProjectContext>(info);

            return buildContext;
        }
    }
}
