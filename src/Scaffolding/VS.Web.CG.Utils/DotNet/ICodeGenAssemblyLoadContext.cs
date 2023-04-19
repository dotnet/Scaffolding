// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;

namespace Microsoft.VisualStudio.Web.CodeGeneration.DotNet
{
    public interface ICodeGenAssemblyLoadContext
    {
        Assembly LoadStream(Stream assembly, Stream symbols);
        Assembly LoadFromName(AssemblyName AssemblyName);
        Assembly LoadFromPath(string path);
    }
}
