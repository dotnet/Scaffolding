// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Shared;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools.Test
{
    public class TargetInstallerTests
    {
        [Fact]
        public void TargetInstallationUsesProjectExtensionsPath()
        {
            var projectDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(projectDirectory);

            try
            {
                var projectPath = Path.Combine(projectDirectory, "test.csproj");
                File.WriteAllText(
                    projectPath,
                    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
                File.WriteAllText(
                    Path.Combine(projectDirectory, "Directory.Build.props"),
                    "<Project><PropertyGroup><UseArtifactsOutput>true</UseArtifactsOutput></PropertyGroup></Project>");

                var installer = new TargetInstaller(new ConsoleLogger());
                installer.EnsureTargetImported(projectPath, "Debug");

                var expectedFilePath = Path.Combine(
                    projectDirectory,
                    "artifacts",
                    "obj",
                    "test",
                    "test.csproj.codegeneration.targets");
                Assert.True(File.Exists(expectedFilePath));
                Assert.False(File.Exists(Path.Combine(projectDirectory, "obj", "test.csproj.codegeneration.targets")));
            }
            finally
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }
}
