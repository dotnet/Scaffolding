// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface IEntityFrameworkService
    {
        [Obsolete]
        Task<ContextProcessingResult> GetModelMetadata(string dbContextFullTypeName, ModelType modelTypeName, string areaName, bool useSqlite);
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
        /// <param name="areaName">Name of the area on which scaffolding is being run. Used for generating path for new DbContext.</param>
        /// <param name="databaseProvider">enum DbProvider (default DbProvider.SqlServer) </param>
        /// <returns>Returns <see cref="ContextProcessingResult"/>.</returns>
        Task<ContextProcessingResult> GetModelMetadata(string dbContextFullTypeName, ModelType modelTypeName, string areaName, DbProvider databaseProvider);
    }
}
