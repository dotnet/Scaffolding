// Copyright (c) .NET Foundation. All rights reserved.

using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils
{
    public class ProjectInformation
    {
        public ProjectInformation(IProjectContext root, IEnumerable<IProjectContext> projectReferences)
        {
            Requires.NotNull(root, nameof(root));
            RootProject = root;

            DependencyProjects = projectReferences ?? new List<IProjectContext>();
        }
        public IProjectContext RootProject { get; }
        public IEnumerable<IProjectContext> DependencyProjects { get; }
    }
}
