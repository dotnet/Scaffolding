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
        private IEnumerable<NuGetFramework> _suggestedFrameworks;
        private NuGetFramework _targetFramework;

        public DotNetProjectContextBuilder(string projectPath, IEnumerable<NuGetFramework> suggestedFrameworks)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (suggestedFrameworks == null || !suggestedFrameworks.Any())
            {
                throw new ArgumentNullException(nameof(suggestedFrameworks));
            }
            _suggestedFrameworks = suggestedFrameworks;
            _projectPath = projectPath;
        }

        public IProjectContext Build()
        {
            ChooseTargetFramework();

            var rootContext = BuildProjectContext(_projectPath, _targetFramework, checkNearest: false);
            var projectRefContexts = new List<IProjectContext>();
            foreach (var projectRef in rootContext.ProjectReferences)
            {
                // For project references, we should still check for the nearest NuGetFramework.
                // The root project can target netcoreapp1.0 and the project references can be targeting 
                // netstandard1.x, so instead of forcing the project reference to create a context with netcoreapp1.0
                // it would end up creating a context for the correct tfm.
                var refContext = BuildProjectContext(projectRef, _targetFramework, checkNearest: true);
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

        private void ChooseTargetFramework()
        {
            var frameworksInProject = ProjectReader.GetProject(_projectPath).GetTargetFrameworks().Select(f => f.FrameworkName);

            var candidateFrameworks = _suggestedFrameworks
                    .Select(tfm =>
                        NuGetFrameworkUtility.GetNearest(
                            frameworksInProject,
                            tfm,
                            f => new NuGetFramework(f)))
                    .Where(framework => framework != null);

            if (!candidateFrameworks.Any())
            {
                var msg = "Could not find a compatible framework to execute."
                            + Environment.NewLine
                            + $"Available frameworks in project:{string.Join($"{Environment.NewLine} -", frameworksInProject.Select(f => f.GetShortFolderName()))}";
                throw new InvalidOperationException(msg);
            }

            _targetFramework = candidateFrameworks.First();
        }

        private DotNetProjectContext BuildProjectContext(string projectPath, NuGetFramework targetFramework, bool checkNearest)
        {
            var projectFile = ProjectReader.GetProject(projectPath);
            var nearestFramework = targetFramework;
            if (checkNearest)
            {
                var frameworksInProject = projectFile.GetTargetFrameworks().Select(f => f.FrameworkName);

                nearestFramework = NuGetFrameworkUtility.GetNearest(
                                    frameworksInProject,
                                    targetFramework,
                                    f => new NuGetFramework(f));
            }

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
