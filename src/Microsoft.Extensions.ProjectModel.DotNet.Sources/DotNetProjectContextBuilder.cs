// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;

namespace Microsoft.Extensions.ProjectModel
{
    public class DotNetProjectContextBuilder
    {
        private string _projectPath;
        private NuGetFramework _targetFramework;

        public DotNetProjectContextBuilder(string projectPath, NuGetFramework targetFramework)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (targetFramework == null)
            {
                throw new ArgumentNullException(nameof(targetFramework));
            }

            _projectPath = projectPath;
            _targetFramework = targetFramework;
        }

        public IProjectContext Build()
        {
            var rootContext = BuildProjectContext(_projectPath, _targetFramework);
            var projectRefContexts = new List<IProjectContext>();
            foreach (var projectRef in rootContext.ProjectReferences)
            {
                var refContext = BuildProjectContext(projectRef, _targetFramework);
                projectRefContexts.Add(refContext);
            }

            var projectReferences = projectRefContexts.Select(
                proj => new ProjectReferenceInformation()
                {
                    ProjectName = proj.ProjectName,
                    AssemblyName = proj.AssemblyName,
                    CompilationItems = proj.CompilationItems,
                    FullPath = proj.ProjectFullPath
                });

            rootContext.ProjectReferenceInformation = projectReferences;

            return rootContext;
        }

        private DotNetProjectContext BuildProjectContext(string projectPath, NuGetFramework targetFramework)
        {
            var projectFile = ProjectReader.GetProject(projectPath);
            var frameworksInProject = projectFile.GetTargetFrameworks().Select(f => f.FrameworkName);

            var nearestFramework = NuGetFrameworkUtility.GetNearest(
                                frameworksInProject,
                                targetFramework,
                                f => new NuGetFramework(f));

            var context = new ProjectContextBuilder()
                .WithProject(projectFile)
                .WithTargetFramework(nearestFramework)
                .Build();

            if (context == null)
            {
                throw new Exception("Failed to get Project Context Information.");
            }

            var rootContext = new DotNetProjectContext(context, "Debug", "", null);
            return rootContext;
        }

    }
}
