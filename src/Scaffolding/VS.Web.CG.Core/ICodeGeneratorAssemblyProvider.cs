// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface ICodeGeneratorAssemblyProvider
    {
        IEnumerable<Assembly> CandidateAssemblies { get; }
    }
}