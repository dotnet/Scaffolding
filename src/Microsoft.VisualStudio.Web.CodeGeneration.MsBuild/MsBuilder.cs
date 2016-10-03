// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using NuGet.Frameworks;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class MsBuilder<T> where T : IMsBuildProcessor
    {
        private readonly string _projectFilePath;
        private T _buildProcessor;

        public MsBuilder(string filePath, T buildProcessor)
        {
            Requires.NotNull(buildProcessor, nameof(buildProcessor));

            _buildProcessor = buildProcessor;
            _buildProcessor.Init();
            _projectFilePath = filePath;
        }

        public void RunMsBuild(NuGetFramework framework)
        {
            var project = CreateProject(_projectFilePath, framework);
            var result = RunMsBuild(project, framework);
            _buildProcessor.ProcessBuildResult(project, result.ProjectStateAfterBuild, result.ResultsByTarget);
        }

        public T BuildProcessor
        {
            get
            {
                return _buildProcessor;
            }
        }

        private BuildResult RunMsBuild(Project project, NuGetFramework framework)
        {
            var projectInstance = project.CreateProjectInstance();
            projectInstance.SetProperty("TargetFramework", framework.Framework);

            var targets = BuildProcessor.Targets;

            var buildRequest = new BuildRequestData(projectInstance, targets, null, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
            var buildParams = new BuildParameters(project.ProjectCollection);

            // Add loggers
            buildParams.Loggers = BuildProcessor.Loggers;
            var result = BuildManager.DefaultBuildManager.Build(buildParams, buildRequest);

            if (result.ProjectStateAfterBuild == null)
            {
                // this is a hack for failed project builds. ProjectStateAfterBuild == null after a failed build
                // But the properties are still available to be read
                result.ProjectStateAfterBuild = projectInstance;
            }

            return result;
        }

        private Project CreateProject(string filePath, NuGetFramework framework)
        {
            var sdkPath = new DotNetSdkResolver().ResolveLatest();
            var msBuildFile = "MSBuild.exe";

            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", Path.Combine(sdkPath, msBuildFile));
            var globalProperties = BuildProcessor.Properties ?? new Dictionary<string, string>();
            if (globalProperties.ContainsKey("TargetFramework"))
            {
                globalProperties.Remove("TargetFramework");
            }

            globalProperties.Add("TargetFramework", framework.GetShortFolderName());

            FillInDefaultProperties(globalProperties);

            return ProjectUtilities.CreateProject(filePath, globalProperties);
        }

        private static void FillInDefaultProperties(Dictionary<string, string> globalProperties)
        {
            // This is currently needed because of /dotnet/cli/issues/4207
            if (!globalProperties.ContainsKey("DotnetHostPath"))
            {
                globalProperties["DotnetHostPath"] = new DotNet.Cli.Utils.Muxer().MuxerPath;
            }

            if (!globalProperties.ContainsKey("MSBuildExtensionsPath"))
            {
                globalProperties["MsBuildExtensionsPath"] = new DotNetSdkResolver().ResolveLatest();
            }
        }
    }
}
