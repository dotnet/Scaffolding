// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{ 
    public class MsBuildProjectFile
    {
        public MsBuildProjectFile(string fullPath,
            IEnumerable<string> sourceFiles,
            IEnumerable<string> projectReferences,
            IEnumerable<string> assemblyReferences,
            IDictionary<string, string> properties,
            string targetFrameworks)
        {
            Requires.NotNullOrEmpty(fullPath, nameof(fullPath));
            Requires.NotNull(sourceFiles, nameof(sourceFiles));
            Requires.NotNull(projectReferences, nameof(projectReferences));
            Requires.NotNull(assemblyReferences, nameof(assemblyReferences));
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(targetFrameworks, nameof(targetFrameworks));

            SourceFiles = sourceFiles;
            ProjectReferences = projectReferences;
            AssemblyReferences = assemblyReferences;
            GlobalProperties = properties;
            FullPath = fullPath;
            TargetFrameworks = targetFrameworks.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public IEnumerable<string> SourceFiles { get; private set; }

        public IEnumerable<string> ProjectReferences { get; private set; }

        public IEnumerable<string> AssemblyReferences { get; private set; }

        public IDictionary<string, string> GlobalProperties { get; private set; }

        public string FullPath { get; private set; }

        public IEnumerable<string> TargetFrameworks { get; private set; }
    }
}
