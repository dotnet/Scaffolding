// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools.Internal
{
    public class DotNetProjectInformationBuilder
    {
        private string _projectPath;
        public DotNetProjectInformationBuilder(string projectPath)
        {
            Requires.NotNull(projectPath, nameof(projectPath));
            _projectPath = projectPath;
        }

        public ProjectInformation Build()
        {
            var rootContext = BuildProjectContext(_projectPath);
            var projectRefContexts = new List<IProjectContext>();
            foreach (var projectRef in rootContext.ProjectReferences)
            {
                var refContext = BuildProjectContext(projectRef);
                projectRefContexts.Add(refContext);
            }

            return new ProjectInformation(rootContext, projectRefContexts);
        }

        private DotNetProjectContext BuildProjectContext(string projectPath)
        {
            var projectFile = ProjectReader.GetProject(projectPath);
            var frameworksInProject = projectFile.GetTargetFrameworks().Select(f => f.FrameworkName);
            var nearestFramework = TargetFrameworkFinder.GetSuitableFrameworkFromProject(frameworksInProject);

            var context = new ProjectContextBuilder()
                .WithProject(projectFile)
                .WithTargetFramework(nearestFramework)
                .Build();

            if (context == null)
            {
                throw new Exception("Failed to get Project Context Information.");
            }

            var rootContext = new DotNetProjectContext(context, "Debug", "");
            return rootContext;
        }

    }
}
