// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface IEntityFrameworkService
    {
        /// <summary>
        /// Gets the EF metadata for given context and model.
        /// Method takes in full type name of context and if there is no context with that name,
        /// attempts to create one. When creating a context, the method also tries to modify Startup
        /// code to register the new context through DI.
        /// When the given context is available but there is no DbSet property of given model type
        /// context will be edited to add the property.
        /// The method throws exceptions if there are any errors running EF code to get the EF metadata.
        /// And no changes are written to disk.
        /// When the method successfully returned, it's guranteed to have ModelMetadata present
        /// in the return value. Before returning all the code edits are persisted to disk.
        /// </summary>
        /// <param name="dbContextFullTypeName">Full name (including namespace) of the context class.</param>
        /// <param name="modelTypeName">Model type for which the EF metadata has to be returned.</param>
        /// <returns>Returns <see cref="ContextProcessingResult"/>.</returns>
        Task<ContextProcessingResult> GetModelMetadata(string dbContextFullTypeName, ModelType modelTypeName);
    }
}