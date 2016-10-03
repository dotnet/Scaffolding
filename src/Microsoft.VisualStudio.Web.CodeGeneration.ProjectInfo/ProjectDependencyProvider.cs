// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class ProjectDependencyProvider: IProjectDependencyProvider
    {
        public ProjectDependencyProvider(Dictionary<string, DependencyDescription> nugetPackages, IEnumerable<ResolvedReference> resolvedReferences)
        {
            Requires.NotNull(nugetPackages, nameof(nugetPackages));
            Requires.NotNull(resolvedReferences, nameof(resolvedReferences));

            NugetPackages = nugetPackages;
            ResolvedReferences = resolvedReferences;
        }
        private Dictionary<string, DependencyDescription> NugetPackages { get; }
        private IEnumerable<ResolvedReference> ResolvedReferences { get; }

        public DependencyDescription GetPackage(string name)
        {
            var dependency = NugetPackages
                ?.FirstOrDefault(p => p.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return dependency?.Value;
        }

        public IEnumerable<DependencyDescription> GetAllPackages()
        {
            return NugetPackages
                ?.Select(p => p.Value);
        }

        public IEnumerable<DependencyDescription> GetReferencingPackages(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return NugetPackages
                ?.Select(p => p.Value)
                .Where(p => p.Dependencies.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }

        public IEnumerable<ResolvedReference> GetAllResolvedReferences()
        {
            return ResolvedReferences;
        }

        public ResolvedReference GetResolvedReference(string name)
        {
            return ResolvedReferences
                ?.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
