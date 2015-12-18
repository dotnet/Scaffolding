// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.CodeGeneration
{
    public class PackageInstaller : IPackageInstaller
    {
        private readonly IApplicationEnvironment _environment;
        private readonly ILogger _logger;
        private const string DependenciesNodeName = "dependencies";
        private readonly IFileSystem _fileSystem;

        public PackageInstaller(
            ILogger logger,
            IApplicationEnvironment environment)
            : this(logger, environment, new DefaultFileSystem())
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }
        }

        internal PackageInstaller(
            ILogger logger,
            IApplicationEnvironment environment,
            IFileSystem fileSystem)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            _logger = logger;
            _environment = environment;
            _fileSystem = fileSystem;
        }

        public Task InstallPackages(IEnumerable<PackageMetadata> packages)
        {
            return Task.Run(() =>
            {
                AddPackages(packages);
                RestorePackages();
            });
        }

        private void RestorePackages()
        {
            try
            {
                // On non-windows, starting a process with the name
                // of a bash file works. However on windows, it doesn't.
                // So we use 'Path' environment variable to find the path
                // of 'dnu.cmd' and start that.
                string fileName;
                if (PlatformHelper.IsMono)
                {
                    fileName = "dnu";
                }
                else
                {
                    fileName = FindDnuCommand();
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = "restore",
                    WorkingDirectory = _environment.ApplicationBasePath,
                    UseShellExecute = false
                };

                var process = Process.Start(startInfo);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                _logger.LogMessage("Error running dnu restore");
                _logger.LogMessage(ex.ToString());
            }
        }

        private string FindDnuCommand()
        {
            var commandName = "dnu.cmd";

            var pathVariable = Environment.GetEnvironmentVariable("Path");
            if (!string.IsNullOrEmpty(pathVariable))
            {
                foreach (var path in pathVariable.Split(';'))
                {
                    var fullPath = Path.Combine(path, commandName);
                    if (_fileSystem.FileExists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            throw new InvalidOperationException(string.Format("{0} {1}", MessageStrings.DnuNotFound, MessageStrings.UnableToRunRestore));
        }

        // Internal for unit tests.
        internal void AddPackages(IEnumerable<PackageMetadata> packages)
        {
            var root = GetProjectJsonContents();
            if (root[DependenciesNodeName] == null)
            {
                root[DependenciesNodeName] = new JObject();
            }

            foreach (var package in packages)
            {
                _logger.LogMessage("Adding dependency " + package.Name +
                    " of version " + package.Version + " to the application.");

                root[DependenciesNodeName][package.Name] = package.Version;
            }

            WriteProjectJsonContents(root);
        }

        private JObject GetProjectJsonContents()
        {
            string projectFile = GetProjectJsonFilePath();
            return JObject.Parse(_fileSystem.ReadAllText(projectFile));
        }

        private void WriteProjectJsonContents(JObject root)
        {
            string projectFile = GetProjectJsonFilePath();
            _fileSystem.WriteAllText(projectFile, root.ToString());
        }

        private string GetProjectJsonFilePath()
        {
            var projectFile = Path.Combine(_environment.ApplicationBasePath, "project.json");
            Debug.Assert(_fileSystem.FileExists(projectFile));
            return projectFile;
        }
    }
}