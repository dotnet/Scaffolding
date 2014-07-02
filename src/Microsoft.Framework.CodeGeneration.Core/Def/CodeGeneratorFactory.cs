// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.CodeGeneration
{
    public abstract class CodeGeneratorFactory
    {
        protected CodeGeneratorFactory([NotNull]CodeGeneratorMetadata codeGeneratorMetadata)
        {
            CodeGeneratorMetadata = codeGeneratorMetadata;
        }

        public CodeGeneratorMetadata CodeGeneratorMetadata { get; private set; }
    }
}