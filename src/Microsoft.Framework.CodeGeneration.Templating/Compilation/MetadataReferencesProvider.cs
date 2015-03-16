// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#if DNX451 || DNXCORE50
using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;
#endif

namespace Microsoft.Framework.CodeGeneration.Templating.Compilation
{
    public class MetadataReferencesProvider
    {
        private List<MetadataReference> _references = new List<MetadataReference>();

#if DNX451 || DNXCORE50
        private static readonly ConcurrentDictionary<string, AssemblyMetadata> _metadataFileCache =
            new ConcurrentDictionary<string, AssemblyMetadata>(StringComparer.OrdinalIgnoreCase);

        private readonly ILibraryManager _libraryManager;
        private readonly IApplicationEnvironment _environment;

        public MetadataReferencesProvider(IApplicationEnvironment environment,
                                          ILibraryManager libraryManager)
        {
            _environment = environment;
            _libraryManager = libraryManager;
        }

        //Todo: This is using application references to compile the template,
        //perhaps that's not right, we should use the dependencies of the caller
        //who calls the templating API.
        public List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();

            // Todo: When the target app references scaffolders as nuget packages rather than project references,
            // since the target app does not actully use code from scaffolding framework, our dlls
            // are not in app dependencies closure. However we need them for compiling the
            // generated razor template. So we are just using the known references for now to make things work.
            // Note that this model breaks for custom scaffolders because they may be using
            // some other references which are not available in any of these closures.
            // As the above comment, the right thing to do here is to use the dependency closure of
            // the assembly which has the template.
            var baseProjects = new string[] { _environment.ApplicationName, "Microsoft.Framework.CodeGeneration" };

            foreach (var baseProject in baseProjects)
            {
                var export = _libraryManager.GetAllExports(baseProject);

                foreach (var metadataReference in export.MetadataReferences)
                {
                    references.Add(ConvertMetadataReference(metadataReference));
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
#else
        public List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();
            references.Add(MetadataReference.CreateFromAssembly(typeof(RazorTemplateBase).Assembly));

            return references;
        }
#endif
    }
}