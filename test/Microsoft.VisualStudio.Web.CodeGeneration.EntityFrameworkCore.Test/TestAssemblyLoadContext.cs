// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class TestAssemblyLoadContext : ICodeGenAssemblyLoadContext
    {
        private ICodeGenAssemblyLoadContext _defaultContext;
        private readonly IProjectDependencyProvider _projectDependencyProvider;

        public TestAssemblyLoadContext(IProjectDependencyProvider projectDependencyProvider)
        {
            _projectDependencyProvider = projectDependencyProvider;
            _defaultContext = new DefaultAssemblyLoadContext();
        }
        public Assembly LoadFromName(AssemblyName AssemblyName)
        {
            var path = _projectDependencyProvider.GetResolvedReference(AssemblyName.Name).ResolvedPath;
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }

        public Assembly LoadStream(Stream assembly, Stream symbols)
        {
            return _defaultContext.LoadStream(assembly, symbols);
        }
    }
}
