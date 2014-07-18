// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.CodeGeneration.EntityFramework
{
    public interface IEntityFrameworkService
    {
        // ToDo: Perhaps this needs to take ITypeSymbol parameters when
        // we need to edit the db context.
        ModelMetadata GetModelMetadata(string dbContextTypeName, string modelTypeName);
    }
}