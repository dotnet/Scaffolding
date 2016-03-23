// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.CodeGeneration.DotNet;


namespace Microsoft.Extensions.CodeGeneration.Templating.Compilation
{
    public class RoslynCompilationService : ICompilationService
    {
        private static readonly ConcurrentDictionary<string, AssemblyMetadata> _metadataFileCache =
            new ConcurrentDictionary<string, AssemblyMetadata>(StringComparer.OrdinalIgnoreCase);

        private readonly ILibraryExporter _libraryExporter;
        private readonly IApplicationEnvironment _environment;
        private readonly ICodeGenAssemblyLoadContext _loader;

        public RoslynCompilationService(IApplicationEnvironment environment,
                                        ICodeGenAssemblyLoadContext loader,
                                        ILibraryExporter libraryExporter)
        {
            if(loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }
            if(environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }
            if(libraryExporter == null)
            {
                throw new ArgumentNullException(nameof(libraryExporter));
            }
            _environment = environment;
            _loader = loader;
            _libraryExporter = libraryExporter;
        }

        public CompilationResult Compile(string content)
        {
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(content) };

            var references = GetApplicationReferences();
            var assemblyName = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(assemblyName,
                        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                        syntaxTrees: syntaxTrees,
                        references: references);


            var result = CommonUtilities.GetAssemblyFromCompilation(_loader, compilation);
            if (result.Success)
            {
                var type = result.Assembly.GetExportedTypes()
                                   .First();

                return CompilationResult.Successful(string.Empty, type);
            }
            else
            {
                return CompilationResult.Failed(content, result.ErrorMessages);
            }
        }

        //Todo: This is using application references to compile the template,
        //perhaps that's not right, we should use the dependencies of the caller
        //who calls the templating API.
        private List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();

            // Todo: When the target app references scaffolders as nuget packages rather than project references,
            // we need to ensure all dependencies for compiling the generated razor template.
            // This requires further thinking for custom scaffolders because they may be using
            // some other references which are not available in any of these closures.
            // As the above comment, the right thing to do here is to use the dependency closure of
            // the assembly which has the template.
            var baseProjects = new string[] { _environment.ApplicationName };

            foreach (var baseProject in baseProjects)
            {
                var exports = _libraryExporter.GetAllExports();

                if (exports != null)
                {
                    foreach (var metadataReference in exports.SelectMany(exp => exp.GetMetadataReferences(throwOnError: false)))
                    {
                        references.Add(metadataReference);
                    }
                }
            }

            return references;
        }
    }
}
