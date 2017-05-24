// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;

namespace Microsoft.VisualStudio.Web.CodeGeneration.DotNet
{
    public partial class DefaultAssemblyLoadContext : ICodeGenAssemblyLoadContext
    {
        public Assembly LoadFromName(AssemblyName AssemblyName)
        {
            return Assembly.Load(AssemblyName);
        }

        public Assembly LoadStream(Stream assembly, Stream symbols)
        {
#if NET461
            using (var ms = new MemoryStream())
            {
                assembly.CopyTo(ms);
                return Assembly.Load(ms.ToArray());
            }
#elif NETSTANDARD2_0
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(assembly);
#else
#error target frameworks need to be updated.
#endif
        }

        public Assembly LoadFromPath(string path)
        {
#if NET461
            return Assembly.LoadFrom(path);
#elif NETSTANDARD2_0
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
#else
#error target frameworks need to be updated.
#endif
            }
    }
}
