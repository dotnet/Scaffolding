// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ProjectModel.Compilation;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public static class LibraryExportExtensions
    {
        public static IEnumerable<MetadataReference> GetMetadataReferences(this LibraryExport export, bool throwOnError = true)
        {
            var references = new List<MetadataReference>();
            AssemblyMetadata assemblyMetadata;
            foreach (var lib in export.CompilationAssemblies)
            {
                try
                {
                    using (var stream = File.OpenRead(lib.ResolvedPath))
                    {
                        var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                        assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                        references.Add(assemblyMetadata.GetReference());
                    }
                }
                catch (Exception ex) 
                    when (ex is FileNotFoundException 
                            || ex is DirectoryNotFoundException
                            || ex is NotSupportedException
                            || ex is ArgumentException
                            || ex is ArgumentOutOfRangeException
                            || ex is BadImageFormatException
                            || ex is IOException
                            || ex is ArgumentNullException)
                {
                    // TODO: Log this
                    if(throwOnError) 
                    {
                        throw ex;
                    }
                    continue;
                }
            }
            return references;
        }
    }
}
