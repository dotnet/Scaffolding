// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface ICodeGeneratorLocator
    {
        IEnumerable<CodeGeneratorDescriptor> CodeGenerators { get; }

        CodeGeneratorDescriptor GetCodeGenerator(string codeGeneratorName);
    }
}
