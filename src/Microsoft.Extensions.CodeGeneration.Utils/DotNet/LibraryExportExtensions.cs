// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ProjectModel.Compilation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public static class LibraryExportExtensions
    {
        public static IEnumerable<MetadataReference> GetMetadataReferences(this LibraryExport export)
        {
            var references = new List<MetadataReference>();
            AssemblyMetadata assemblyMetadata;
            foreach (var lib in export.CompilationAssemblies)
            {
                using (var stream = File.OpenRead(lib.ResolvedPath))
                {
                    var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                    assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                    references.Add(assemblyMetadata.GetReference());
                }
            }
            return references;
        }
    }
}
