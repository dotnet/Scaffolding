// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class TestAssemblyLoadContext : ICodeGenAssemblyLoadContext
    {
        private ICodeGenAssemblyLoadContext _defaultContext;
        private readonly IProjectContext _projectContext;

        public TestAssemblyLoadContext(IProjectContext projectDependencyProvider)
        {
            _projectContext = projectDependencyProvider;
            _defaultContext = new DefaultAssemblyLoadContext();
        }
        public Assembly LoadFromName(AssemblyName AssemblyName)
        {
            var path = _projectContext.CompilationAssemblies.First(c => Path.GetFileNameWithoutExtension(c.Name) == AssemblyName.Name).ResolvedPath;
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }

        public Assembly LoadFromPath(string path)
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }

        public Assembly LoadStream(Stream assembly, Stream symbols)
        {
            return _defaultContext.LoadStream(assembly, symbols);
        }
    }
}
