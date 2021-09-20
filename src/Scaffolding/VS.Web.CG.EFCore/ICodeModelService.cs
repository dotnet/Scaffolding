// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Scaffolding.Shared.Project;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface ICodeModelService
    {
        /// <summary>
        /// Gets the metadata for a given model without using the Datacontext.
        /// Without the datacontext, the metadata will consist of no Navigations/ Primary keys.
        /// </summary>
        /// <param name="modelType">Model type for the EF metadata has to be returned.</param>
        /// <returns>Returns <see cref="ContextProcessingResult"/>.</returns>
        Task<ContextProcessingResult> GetModelMetadata(ModelType modelType);
    }
}
