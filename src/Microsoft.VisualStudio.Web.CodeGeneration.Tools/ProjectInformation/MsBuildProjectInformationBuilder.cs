// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using System.Linq;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools.Internal
{
    public class MsBuildProjectInformationBuilder
    {
        private string _projectPath;
        private HashSet<string> _addedProjects;
        public MsBuildProjectInformationBuilder(string projectPath)
        {
            Requires.NotNull(projectPath, nameof(projectPath));
            _projectPath = projectPath;
        }

        /* 
         * This is very expensive. (For every csproj, we end up invoking a design time build and resolve its references for all TFMs in the project)
         * Building the dependency projects is needed only for building a RoslynWorkspace.
         * Also all of this information will be serialized and sent over a TCP socket to the Inside man.
         * Once we have the 'msbuild workspace' working in .net core, we can get rid of all this evaluations.
         */
        public ProjectInformation Build()
        {
            _addedProjects = new HashSet<string>();
            var rootContext = BuildProjectContext(_projectPath);
            _addedProjects.Add(_projectPath);
            var projectRefContexts = new List<IProjectContext>();
            var dependencyProjects = new Queue<string>();
            foreach (var projectRef in rootContext.ProjectReferences)
            {
                dependencyProjects.Enqueue(projectRef);
            }

            while (dependencyProjects.Count > 0)
            {
                var currentProject = dependencyProjects.Dequeue();
                if (!_addedProjects.Contains(currentProject))
                {
                    _addedProjects.Add(currentProject);
                    var refContext = BuildProjectContext(currentProject);
                    projectRefContexts.Add(refContext);
                    foreach(var dependency in refContext.ProjectReferences)
                    {
                        dependencyProjects.Enqueue(dependency);
                    }
                }
            }

            return new ProjectInformation(rootContext, projectRefContexts);
        }

        // TODO: Need a way to figure out the TargetFrameworks available in the project.
        // Add APIs to Extensions.ProjectModel.MsBuild.Sources?
        private MsBuildProjectContext BuildProjectContext(string projectPath)
        {
            var contexts =  new MsBuildProjectContextBuilder()
                .AsDesignTimeBuild()
                .WithConfiguration("Debug")
                .WithProjectFile(projectPath)
                .BuildAllTargetFrameworks()
                .ToList();

            var nearestFramework = TargetFrameworkFinder.GetSuitableFrameworkFromProject(contexts.Select(c => c.TargetFramework));

            return contexts.First(c => c.TargetFramework.DotNetFrameworkName.Equals(nearestFramework.DotNetFrameworkName));
        }
    }
}
