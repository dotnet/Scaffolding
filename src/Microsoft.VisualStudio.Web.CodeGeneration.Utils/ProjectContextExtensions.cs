// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils
{
    public static class ProjectContextExtensions
    {
        public static DependencyDescription GetPackage(this IProjectContext context, string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNull(context, nameof(context));

            return context.PackageDependencies.FirstOrDefault(package => package.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<DependencyDescription> GetReferencingPackages(this IProjectContext context, string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNull(context, nameof(context));

            return context
                .PackageDependencies
                .Where(package => package
                    .Dependencies
                    .Any(dep => dep.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
