// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
