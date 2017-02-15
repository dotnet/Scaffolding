// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
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
