// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore 
{
    internal class ReflectedTypesProvider
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
            if (compilation ==null)
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
            // On NET451 if the assembly name is different, MVC fails to load the assembly as it is not found on disk.

#if NET451
            var newAssemblyName = Path.GetFileNameWithoutExtension(compilation.AssemblyName);
#else
            var newAssemblyName = Path.GetRandomFileName();
#endif

            var newCompilation = compilation
                .WithAssemblyName(newAssemblyName)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var result = CommonUtilities.GetAssemblyFromCompilation(_loader, newCompilation);
            return result;
        }

        public Type GetReflectedType(string modelType)
        {
            return GetReflectedType(modelType, false);
        }

        public Type GetReflectedType(string modelType, bool lookInDependencies)
        {
            if(string.IsNullOrEmpty(modelType))
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

        // Look for the model type in the current project. 
        // If its not found in the current project, look in the dependencies.
        private Type GetTypeFromAssembly(string modelTypeName, Assembly assembly, bool lookInDependencies)
        {
            if (_compilationResult == null
                || !_compilationResult.Success)
            {
                // If the compilation was not successful, just return null.
                return null;
            }

            if(string.IsNullOrEmpty(modelTypeName))
            {
                throw new ArgumentNullException(nameof(modelTypeName));
            }

            if(assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Type modelType = null;
            try
            {
                modelType = assembly.GetType(modelTypeName);
            }
            catch (Exception ex)
            {
                _logger.LogMessage(ex.Message, LogMessageLevel.Error);
            }

            if (modelType == null && lookInDependencies)
            {
                // Need to look in the dependencies of this project now.
                var dependencies = _projectContext.CompilationAssemblies.GetEnumerator();
                while (modelType == null && dependencies.MoveNext())
                {
                    try
                    {
                        // Since we are running in the project's dependency context, loading assemblies
                        // by name just works.
                        var dAssemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(dependencies.Current.ResolvedPath));
                        var dAssembly = _loader.LoadFromName(dAssemblyName);
                        modelType = dAssembly.GetType(modelTypeName);
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

            return modelType;
        }
    }
}