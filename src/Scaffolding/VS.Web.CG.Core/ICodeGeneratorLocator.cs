// Copyright (c) .NET Foundation. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface ICodeGeneratorLocator
    {
        IEnumerable<CodeGeneratorDescriptor> CodeGenerators { get; }

        CodeGeneratorDescriptor GetCodeGenerator(string codeGeneratorName);
    }
}
