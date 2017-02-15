// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel
{
    public class ResolvedReference
    {
        public ResolvedReference(string name, string resolvedPath)
        {
            Name = name;
            ResolvedPath = resolvedPath;
        }
        public string ResolvedPath { get; }
        public string Name { get; }
    }
}
