// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.EntityFramework;

namespace Microsoft.Extensions.CodeGenerators.Mvc
{
    public class ModelTypeAndContextModel
    {
        public ModelType ModelType { get; set; }

        public ContextProcessingResult ContextProcessingResult { get; set; }

        public string DbContextFullName { get; set; }
    }
}
