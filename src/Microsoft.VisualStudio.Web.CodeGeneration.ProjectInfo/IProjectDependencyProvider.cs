// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public interface IProjectDependencyProvider
    {
        IEnumerable<DependencyDescription> GetAllPackages();
        IEnumerable<ResolvedReference> GetAllResolvedReferences();
        DependencyDescription GetPackage(string name);
        IEnumerable<DependencyDescription> GetReferencingPackages(string name);
        ResolvedReference GetResolvedReference(string name);
    }
}