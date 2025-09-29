// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings
{
    /// <summary>
    /// Settings for Entra ID scaffolding steps, including user, project, and application information.
    /// </summary>
    internal class EntraIdSettings
    {
        /// <summary>
        /// The username for Entra ID operations.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// The path to the project file.
        /// </summary>
        public string? Project { get; set; }
        /// <summary>
        /// The tenant ID for Entra ID.
        /// </summary>
        public string? TenantId { get; set; }
        /// <summary>
        /// The application name or ID for Entra ID.
        /// </summary>
        public string? Application { get; set; }
        /// <summary>
        /// Option to select the application interactively.
        /// </summary>
        public string? SelectApplication { get; set; }
    }
}
