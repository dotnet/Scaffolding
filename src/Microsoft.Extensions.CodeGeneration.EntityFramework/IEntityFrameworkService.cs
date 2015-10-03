// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework
{
    public interface IEntityFrameworkService
    {
        // ToDo: Perhaps this needs to take ITypeSymbol parameters when
        // we need to edit the db context.
        Task<ModelMetadata> GetModelMetadata(string dbContextFullTypeName, ModelType modelTypeName);
    }
}