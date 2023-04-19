// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared.Project;

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
