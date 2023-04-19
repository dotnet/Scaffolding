﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.DotNet
{
    public static class ResolvedReferenceExtensions
    {
        [SuppressMessage("supressing re-throw exception", "CA2200")]
        public static IEnumerable<MetadataReference> GetMetadataReference(this ResolvedReference reference, bool throwOnError = true)
        {
            var references = new List<MetadataReference>();
            AssemblyMetadata assemblyMetadata;
            try
            {
                using (var stream = File.OpenRead(reference.ResolvedPath))
                {
                    var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                    assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                    references.Add(assemblyMetadata.GetReference());
                }
            }
            catch (Exception ex)
                when (ex is NotSupportedException
                        || ex is ArgumentException
                        || ex is BadImageFormatException
                        || ex is IOException)
            {
                // TODO: Log this
                if (throwOnError)
                {
                    throw ex;
                }
            }
            return references;
        }
    }
}
