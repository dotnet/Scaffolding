// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Dnx.Runtime;

namespace Microsoft.Framework.CodeGeneration.Templating.Compilation
{
    public class RoslynCompilationService : ICompilationService
    {
        private static readonly ConcurrentDictionary<string, AssemblyMetadata> _metadataFileCache =
            new ConcurrentDictionary<string, AssemblyMetadata>(StringComparer.OrdinalIgnoreCase);

        private readonly ILibraryExporter _libraryExporter;
        private readonly IApplicationEnvironment _environment;
        private readonly IAssemblyLoadContext _loader;

        public RoslynCompilationService(IApplicationEnvironment environment,
                                        IAssemblyLoadContextAccessor accessor,
                                        ILibraryExporter libraryExporter)
        {
            _environment = environment;
            _loader = accessor.GetLoadContext(typeof(RoslynCompilationService).GetTypeInfo().Assembly);
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
                var export = _libraryExporter.GetAllExports(baseProject);

                if (export != null)
                {
                    foreach (var metadataReference in export.MetadataReferences)
                    {
                        references.Add(ConvertMetadataReference(metadataReference));
                    }
                }
            }

            return references;
        }

        private MetadataReference ConvertMetadataReference(IMetadataReference metadataReference)
        {
            var roslynReference = metadataReference as IRoslynMetadataReference;

            if (roslynReference != null)
            {
                return roslynReference.MetadataReference;
            }

            var embeddedReference = metadataReference as IMetadataEmbeddedReference;

            if (embeddedReference != null)
            {
                return MetadataReference.CreateFromImage(embeddedReference.Contents);
            }

            var fileMetadataReference = metadataReference as IMetadataFileReference;

            if (fileMetadataReference != null)
            {
                return CreateMetadataFileReference(fileMetadataReference.Path);
            }

            var projectReference = metadataReference as IMetadataProjectReference;
            if (projectReference != null)
            {
                using (var ms = new MemoryStream())
                {
                    projectReference.EmitReferenceAssembly(ms);

                    return MetadataReference.CreateFromImage(ms.ToArray());
                }
            }

            throw new NotSupportedException();
        }

        private MetadataReference CreateMetadataFileReference(string path)
        {
            var metadata = _metadataFileCache.GetOrAdd(path, _ =>
            {
                return AssemblyMetadata.CreateFromStream(File.OpenRead(path));
            });

            return metadata.GetReference();
        }
    }
}
