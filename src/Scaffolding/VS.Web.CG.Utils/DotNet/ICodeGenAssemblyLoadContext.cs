// Copyright (c) .NET Foundation. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.DotNet
{
    public interface ICodeGenAssemblyLoadContext
    {
        Assembly LoadStream(Stream assembly, Stream symbols);
        Assembly LoadFromName(AssemblyName AssemblyName);
        Assembly LoadFromPath(string path);
    }
}
