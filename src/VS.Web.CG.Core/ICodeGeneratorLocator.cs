// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface ICodeGeneratorLocator
    {
        IEnumerable<CodeGeneratorDescriptor> CodeGenerators { get; }

        CodeGeneratorDescriptor GetCodeGenerator(string codeGeneratorName);
    }
}