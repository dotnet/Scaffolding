using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.DotNet
{
    public static class ResolvedReferenceExtensions
    {
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
                if (throwOnError)
                {
                    throw ex;
                }
            }
            return references;
        }
    }
}
