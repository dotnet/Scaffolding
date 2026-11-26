// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.Extensions.Internal;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    public class TargetInstaller
    {
        private ILogger _logger;

        public TargetInstaller(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        public bool EnsureTargetImported(string projectPath, string configuration, string workingDirectory)
        {
            const string ToolsImportTargetsName = "Imports.targets";

            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (string.IsNullOrEmpty(configuration))
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrEmpty(workingDirectory))
            {
                throw new ArgumentNullException(nameof(workingDirectory));
            }

            var targetLocation = GetProjectExtensionsPath(projectPath, workingDirectory, configuration);
            Directory.CreateDirectory(targetLocation);

            var projectName = Path.GetFileName(projectPath);
            var fileName = $"{projectName}.codegeneration.targets";
            var importingTargetFilePath = Path.Combine(targetLocation, fileName);

            if (File.Exists(importingTargetFilePath))
            {
                return true;
            }

            var toolType = typeof(TargetInstaller);
            var toolAssembly = toolType.GetTypeInfo().Assembly;
            var toolNamespace = toolType.Namespace;
            var toolImportTargetsResourceName = $"{toolNamespace}.compiler.resources.{ToolsImportTargetsName}";

            using (var stream = toolAssembly.GetManifestResourceStream(toolImportTargetsResourceName))
            {
                var targetBytes = new byte[stream.Length];
                stream.ReadExactly(targetBytes);
                File.WriteAllBytes(importingTargetFilePath, targetBytes);
            }
            return true;
        }

        private static string GetProjectExtensionsPath(string projectPath, string workingDirectory, string configuration)
        {
            var output = new List<string>();
            var errors = new List<string>();
            var result = Command.CreateDotNet(
                "msbuild",
                new[]
                {
                    projectPath,
                    $"/p:Configuration={configuration}",
                    "-nologo",
                    "-getProperty:MSBuildProjectExtensionsPath"
                })
                .InWorkingDirectory(workingDirectory)
                .OnErrorLine(error => errors.Add(error))
                .OnOutputLine(line => output.Add(line))
                .Execute();

            var extensionsPath = output.LastOrDefault(line => !string.IsNullOrWhiteSpace(line))?.Trim();
            if (result.ExitCode != 0 || string.IsNullOrEmpty(extensionsPath))
            {
                var errorMessage = $"Failed to resolve MSBuildProjectExtensionsPath for {projectPath}.";
                if (errors.Count > 0)
                {
                    errorMessage += $"{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
                }

                throw new InvalidOperationException(errorMessage);
            }

            return Path.GetFullPath(extensionsPath, workingDirectory);
        }
    }
}
