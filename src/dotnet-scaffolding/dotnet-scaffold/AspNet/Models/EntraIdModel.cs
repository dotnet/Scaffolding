// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models
{
    /// <summary>
    /// Represents the model for Entra ID scaffolding, containing project and application information.
    /// </summary>
    internal class EntraIdModel
    {
        /// <summary>
        /// Gets or sets the project information.
        /// </summary>
        public ProjectInfo? ProjectInfo { get; set; }
        /// <summary>
        /// Gets or sets the username for Entra ID.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Gets or sets the tenant ID for Entra ID.
        /// </summary>
        public string? TenantId { get; set; }
        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string? Application { get; set; }
        /// <summary>
        /// Gets or sets whether to use existing application.
        /// </summary>
        public bool UseExistingApplication { get; set; }
        /// <summary>
        /// Gets or sets the base output path for generated files.
        /// </summary>
        public string? BaseOutputPath { get; set; }
        /// <summary>
        /// Gets or sets the namespace for Entra ID related code.
        /// </summary>
        public string? EntraIdNamespace { get; set; }
    }
}
