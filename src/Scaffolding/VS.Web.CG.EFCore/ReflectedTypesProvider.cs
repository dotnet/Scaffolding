// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class ReflectedTypesProvider
    {
        private ILogger _logger;
        private ICodeGenAssemblyLoadContext _loader;
        private Compilation _compilation;
        private CompilationResult _compilationResult;
        private IProjectContext _projectContext;

        public ReflectedTypesProvider(Compilation compilation,
             Func<Compilation, Compilation> compilationModificationFunc,
             IProjectContext projectContext,
             ICodeGenAssemblyLoadContext loader,
             ILogger logger)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            _projectContext = projectContext;
            _logger = logger;
            _loader = loader;
            _compilation = compilationModificationFunc == null
                 ? compilation
                 : compilationModificationFunc(compilation);
            _compilationResult = GetCompilationResult(_compilation);
        }

        private CompilationResult GetCompilationResult(Compilation compilation)
        {
            // Need these #ifdefs as coreclr needs the assembly name to be different to be loaded from stream.
            // On desktop if the assembly name is different, MVC fails to load the assembly as it is not found on disk.

            var newAssemblyName = Path.GetFileNameWithoutExtension(compilation.AssemblyName);

            var newCompilation = compilation
                .WithAssemblyName(newAssemblyName)
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            var result = CommonUtilities.GetAssemblyFromCompilation(_loader, newCompilation);
            return result;
        }

        public Type[] GetAllTypesInProject(bool throwOnError = false)
        {
            if (_compilationResult == null
                || !_compilationResult.Success)
            {
                // If the compilation was not successful, just return null.
                return null;
            }

            try
            {
                return _compilationResult.Assembly?.GetTypes();
            }
            catch (Exception ex)
            {
                _logger.LogMessage(ex.Message, LogMessageLevel.Error);
                if (throwOnError)
                {
                    throw;
                }
            }

            return null;
        }

        public Type GetReflectedType(string modelType)
        {
            return GetReflectedType(modelType, false);
        }

        public Type GetReflectedType(string modelType, bool lookInDependencies)
        {
            if (string.IsNullOrEmpty(modelType))
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (_compilationResult == null
                || !_compilationResult.Success)
            {
                // If the compilation was not successful, just return null.
                return null;
            }

            Type modelReflectedType = null;
            try
            {
                modelReflectedType = _compilationResult.Assembly?.GetType(modelType);
            }
            catch (Exception ex)
            {
                _logger.LogMessage(ex.Message, LogMessageLevel.Error);
            }

            if (modelReflectedType == null && lookInDependencies)
            {
                // Need to look in the dependencies of this project now.
                var dependencies = _projectContext.CompilationAssemblies.GetEnumerator();
                while (modelReflectedType == null && dependencies.MoveNext())
                {
                    try
                    {
                        // Since we are running in the project's dependency context, loading assemblies
                        // by name just works.
                        var dAssemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(dependencies.Current.ResolvedPath));
                        var dAssembly = _loader.LoadFromName(dAssemblyName);
                        modelReflectedType = dAssembly.GetType(modelType);
                    }
                    catch (Exception ex)
                    {
                        // This is a best effort approach. If we cannot load an assembly for any reason,
                        // just ignore it and look for the type in the next one.
                        _logger.LogMessage(ex.Message, LogMessageLevel.Trace);
                        continue;
                    }

                }
            }

            return modelReflectedType;
        }

        public IEnumerable<string> GetCompilationErrors()
        {
            return _compilationResult?.ErrorMessages;
        }
    }
}
