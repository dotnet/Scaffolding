// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    internal class ProjectFileFinder
    {
        private string _initialPath;
        public ProjectFileFinder(string path)
        {
            _initialPath = string.IsNullOrEmpty(path)
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(path);

            Resolve();
        }

        private void Resolve()
        {
            string resolvedPath = _initialPath;
            var isDirectory = !File.Exists(_initialPath) && Directory.Exists(_initialPath);
            if (isDirectory)
            {
                var csprojFiles = Directory.EnumerateFiles(_initialPath, "*.csproj");
                var projectJson = Path.Combine(_initialPath, "project.json");

                if ((File.Exists(projectJson) && csprojFiles.Any())
                    ||csprojFiles.Count() > 1)
                {
                    throw new Exception($"Multiple Project files found in the directory {_initialPath}. Please provide full path to the file to use.");
                }

                if (csprojFiles.Any())
                {
                    resolvedPath = csprojFiles.First();
                }
                else
                {
                    resolvedPath = projectJson;
                }
            }

            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException($"Could not find project file in {_initialPath}");
            }

            if (!Path.GetExtension(resolvedPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                && !Path.GetFileName(resolvedPath).Equals("project.json", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Invalid project file {resolvedPath}");
            }

            ProjectFilePath = resolvedPath;
        }

        public bool IsMsBuildProject
        {
            get
            {
                return Path.GetExtension(ProjectFilePath).Equals(".csproj", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string ProjectFilePath { get; private set; }
    }
}
