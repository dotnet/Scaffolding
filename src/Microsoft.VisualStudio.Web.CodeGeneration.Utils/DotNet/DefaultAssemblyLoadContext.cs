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
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(assembly);
        }

        public Assembly LoadFromPath(string path)
        {
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }
    }
}
