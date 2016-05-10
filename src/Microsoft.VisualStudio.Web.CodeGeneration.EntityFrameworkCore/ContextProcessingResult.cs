// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    /// <summary>
    /// Represents the result of obtaining EF metadata for a context and a model type.
    /// </summary>
    public class ContextProcessingResult
    {
        /// <summary>
        /// An enumeration representing what kind of processing was done for the
        /// given context name.
        /// </summary>
        public ContextProcessingStatus ContextProcessingStatus { get; set; }

        /// <summary>
        /// EF metadata to be used for generating views / controller code.
        /// </summary>
        public ModelMetadata ModelMetadata { get; set; }
    }

    /// <summary>
    /// Represents the status of EF DbContext processing.
    /// </summary>
    public enum ContextProcessingStatus
    {
        /// <summary>
        /// No edits were required to DbContext
        /// </summary>
        ContextAvailable = 1,

        /// <summary>
        /// A new context was created and succefully configured through DI.
        /// </summary>
        ContextAdded = 2,

        /// <summary>
        /// A new context was created however requires some more configuration changes for DI to work.
        /// </summary>
        ContextAddedButRequiresConfig = 3,

        /// <summary>
        /// A context was available but it was edited to add DbSet property.
        /// </summary>
        ContextEdited = 4
    }
}
