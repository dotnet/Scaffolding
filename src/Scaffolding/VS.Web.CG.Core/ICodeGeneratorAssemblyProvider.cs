// Copyright (c) .NET Foundation. All rights reserved.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface ICodeGeneratorAssemblyProvider
    {
        IEnumerable<Assembly> CandidateAssemblies { get; }
    }
}
