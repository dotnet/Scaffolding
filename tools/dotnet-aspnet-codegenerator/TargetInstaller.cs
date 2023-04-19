// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Shared;

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

        public bool EnsureTargetImported(string projectName, string targetLocation)
        {
            const string ToolsImportTargetsName = "Imports.targets";

            if (string.IsNullOrEmpty(projectName))
            {
                throw new ArgumentNullException(nameof(projectName));
            }

            if (string.IsNullOrEmpty(targetLocation))
            {
                throw new ArgumentNullException(nameof(targetLocation));
            }

            // Create the directory structure if it doesn't exist.
            Directory.CreateDirectory(targetLocation);

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
                stream.Read(targetBytes, 0, targetBytes.Length);
                File.WriteAllBytes(importingTargetFilePath, targetBytes);
            }
            return true;
        }
    }
}
