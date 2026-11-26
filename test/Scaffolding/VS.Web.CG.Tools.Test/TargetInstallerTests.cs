// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.Extensions.ProjectModel;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools.Test
{
    public class TargetInstallerTests
    {
        [Fact]
        public void TargetInstallationUsesProjectExtensionsPath()
        {
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var projectDirectory = Path.Combine(workingDirectory, "src", "app");
            Directory.CreateDirectory(projectDirectory);

            try
            {
                Directory.CreateDirectory(workingDirectory);
                var projectPath = Path.Combine(projectDirectory, "test.csproj");
                File.WriteAllText(
                    projectPath,
                    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
                File.WriteAllText(
                    Path.Combine(workingDirectory, "Directory.Build.props"),
                    "<Project><PropertyGroup><UseArtifactsOutput>true</UseArtifactsOutput><ArtifactsPath>$(MSBuildStartupDirectory)/artifacts</ArtifactsPath></PropertyGroup></Project>");

                var installer = new TargetInstaller(new ConsoleLogger());
                installer.EnsureTargetImported(projectPath, "Debug", workingDirectory);

                var expectedFilePath = Path.Combine(
                    workingDirectory,
                    "artifacts",
                    "obj",
                    "test",
                    "test.csproj.codegeneration.targets");
                Assert.True(File.Exists(expectedFilePath));
                Assert.False(File.Exists(Path.Combine(projectDirectory, "artifacts", "obj", "test", "test.csproj.codegeneration.targets")));

                var targetsLocation = Path.Combine(workingDirectory, "targets");
                var buildDirectory = Path.Combine(targetsLocation, "build");
                Directory.CreateDirectory(buildDirectory);
                var projectContext = System.Security.SecurityElement.Escape(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new { ProjectFullPath = projectPath }));
                File.WriteAllText(
                    Path.Combine(buildDirectory, "microsoft.visualstudio.web.codegeneration.tools.targets"),
                    "<Project><Target Name=\"EvaluateProjectInfoForCodeGeneration\">" +
                    $"<WriteLinesToFile File=\"$(OutputFile)\" Lines=\"{projectContext}\" Overwrite=\"true\" />" +
                    "</Target></Project>");
                var context = new MsBuildProjectContextBuilder(
                    projectPath,
                    targetsLocation,
                    "Debug",
                    workingDirectory).Build();

                Assert.Equal(projectPath, context.ProjectFullPath);
            }
            finally
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
    }
}
