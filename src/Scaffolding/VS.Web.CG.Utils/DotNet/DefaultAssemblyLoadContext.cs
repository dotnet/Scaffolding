// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            using (var ms = new MemoryStream())
            {
                assembly.CopyTo(ms);
                return Assembly.Load(ms.ToArray());
            }
        }

        public Assembly LoadFromPath(string path)
        {
            return Assembly.LoadFrom(path);
        }
    }
}
