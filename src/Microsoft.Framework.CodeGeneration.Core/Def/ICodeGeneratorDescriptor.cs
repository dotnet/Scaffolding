// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.Framework.CodeGeneration
{
    public interface ICodeGeneratorDescriptor
    {
        string Name { get; }

        IActionDescriptor CodeGeneratorAction { get; }

        object CodeGeneratorInstance { get; }
    }
}